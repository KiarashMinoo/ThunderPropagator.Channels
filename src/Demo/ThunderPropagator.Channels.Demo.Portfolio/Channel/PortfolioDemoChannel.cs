using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Exceptions;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Demo.Portfolio.Configuration;
using ThunderPropagator.Channels.Demo.Portfolio.Messages;
using ThunderPropagator.Channels.Demo.Portfolio.Metadata;

namespace ThunderPropagator.Channels.Demo.Portfolio.Channel
{
    public
#if !DEBUG
        sealed
#endif
        partial class PortfolioDemoChannel : AbstractChannel<PortfolioDemoChannelMetadata, PortfolioDemoChannelConfiguration>
    {
        public const string PortfolioDemo = nameof(PortfolioDemo);
        public const string PortfolioDemoItems = nameof(PortfolioDemoItems);

        private readonly CancellationToken _cancellationToken;
        private readonly ILogger<PortfolioDemoChannel> _logger;

        public PortfolioDemoChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _cancellationToken = serviceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            _logger = serviceProvider.GetRequiredService<ILogger<PortfolioDemoChannel>>();

            // Issue #11: SimulateAsync's own returned Task is deliberately never awaited here (a
            // constructor can't be async) — but it must still be observed, not merely fired and
            // forgotten, so a fault inside the loop is logged instead of silently disappearing (the
            // previous async void Simulate() either crashed the process via the synchronization context
            // in ASP.NET Core, or vanished with no trace in other hosts). OnlyOnFaulted deliberately
            // excludes the loop's own normal exit path: an unhandled OperationCanceledException escaping
            // an async method always completes its Task as Canceled, never Faulted, regardless of which
            // token it carries — so graceful shutdown (the loop's while condition/Task.Delay observing
            // _cancellationToken) never logs an error here.
            _ = SimulateAsync().ContinueWith(
                task => Log.SimulationLoopFaulted(_logger, task.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        internal static decimal GeneratePrice() => new Faker().Random.Decimal(1M, 100M);

        /// <summary>
        /// The subscribing <see cref="PortfolioDemoChannelFeederMessage.Key"/> <paramref name="connectionId"/>
        /// is currently subscribed under, or <see langword="null"/> if it is not (or no longer)
        /// subscribed to this channel. Issue #36's own fix: Buy/Sell resolve their target portfolio
        /// entry through this rather than trusting a caller-supplied Key in the request body, so a
        /// connection can only ever buy/sell against the position it subscribed to create, never an
        /// arbitrary other subscriber's.
        /// </summary>
        internal string? FindSubscribedKey(string connectionId) =>
            Subscriptions.Subscriptions
                .FirstOrDefault(subscription => subscription.ConnectionInfo.ConnectionId == connectionId)
                ?.SubscribedPrograms.SubscribedKeys[nameof(PortfolioDemoChannelFeederMessage.Key)];

        // Issue #14: Subscribe()/Unsubscribe() (which invoke these hooks) are synchronous,
        // non-awaitable base-class APIs with no async counterpart, and the only snapshot search
        // available is Task-returning — so the search and its follow-up work can't be awaited here
        // without blocking the calling thread. Fire-and-forget mirrors the pattern already used for
        // NotificationsChannel.OnSubscriptionAdded (#58) and this channel's own constructor (#11):
        // failures are caught and logged instead of propagating synchronously. The trade-off is that
        // the duplicate-key check and the deleted-item cleanup emission below now complete
        // asynchronously (after Subscribe()/Unsubscribe() has already returned) rather than
        // synchronously gating the caller.
        protected override void OnSubscriptionAdded(Subscription subscription)
        {
            base.OnSubscriptionAdded(subscription);

            var key = subscription.SubscribedPrograms.SubscribedKeys[nameof(PortfolioDemoChannelFeederMessage.Key)];

            _ = HandleSubscriptionAddedAsync(key).ContinueWith(
                task => Log.SubscriptionAddedFailed(_logger, task.Exception, key, Metadata.ChannelName),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        protected override void OnSubscriptionRemoved(Subscription subscription)
        {
            base.OnSubscriptionRemoved(subscription);

            var key = subscription.SubscribedPrograms.SubscribedKeys[nameof(PortfolioDemoChannelFeederMessage.Key)];

            _ = HandleSubscriptionRemovedAsync(key).ContinueWith(
                task => Log.SubscriptionRemovedFailed(_logger, task.Exception, key, Metadata.ChannelName),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        private async Task HandleSubscriptionAddedAsync(string key)
        {
            var snapshotEntries = await SearchSnapshotsAsync(snapshotEntry => snapshotEntry.Snapshot.ContainsKey(key), 0, 0, _cancellationToken).ConfigureAwait(false);

            if (snapshotEntries.Length > 0)
                throw new DuplicatedKeyException(nameof(PortfolioDemoChannelFeederMessage.Key), key);

            var portfolioDemoChannelFeederMessages = new Faker<PortfolioDemoChannelFeederMessage>()
                .RuleFor(x => x.Key, key)
                .RuleFor(x => x.Stock, faker => faker.Company.CompanyName())
                .RuleFor(x => x.Price, GeneratePrice())
                .RuleFor(x => x.Quantity, faker => faker.Random.Int(1, 1000))
                .RuleFor(x => x.Time, DateTime.UtcNow.TimeOfDay)
                .Generate(Random.Shared.Next(5, 30))
                .ToArray();

            foreach (var portfolioDemoChannelFeederMessage in portfolioDemoChannelFeederMessages)
                await EmitMessageAsync(portfolioDemoChannelFeederMessage, _cancellationToken);
        }

        private async Task HandleSubscriptionRemovedAsync(string key)
        {
            var snapshotEntries = await SearchSnapshotsAsync(snapshotEntry => snapshotEntry.Snapshot.ContainsKey(key), 0, 0, _cancellationToken).ConfigureAwait(false);

            foreach (var snapshotEntry in snapshotEntries)
            {
                PortfolioDemoChannelFeederMessage portfolioDemoChannelFeederMessage = new(snapshotEntry.Snapshot);
                portfolioDemoChannelFeederMessage.IsDeleted = true;
                EmitMessage(snapshotEntry.HashKey, CastType.Unicast, portfolioDemoChannelFeederMessage, typeof(PortfolioDemoChannelFeederMessage));
            }
        }

        private async Task SimulateAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                var minMilliseconds = (int)ChannelConfiguration.MinPollInterval.TotalMilliseconds;
                var maxMilliseconds = (int)ChannelConfiguration.MaxPollInterval.TotalMilliseconds;
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(minMilliseconds, maxMilliseconds)), _cancellationToken);

                var snapshotEntries = await SearchSnapshotsAsync(_ => true, 0, 0, _cancellationToken);

                if (snapshotEntries.Length == 0)
                    continue;

                snapshotEntries = snapshotEntries
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(Random.Shared.Next(1, snapshotEntries.Length))
                    .ToArray();

                foreach (var snapshotEntry in snapshotEntries)
                {
                    PortfolioDemoChannelFeederMessage portfolioDemoChannelFeederMessage = new(snapshotEntry.Snapshot);

                    var faker = new Faker();
                    var increase = faker.Random.Bool();
                    var factor = faker.Random.Decimal(.01M, .05M);
                    var price = portfolioDemoChannelFeederMessage.Price;
                    var value = price * factor;
                    portfolioDemoChannelFeederMessage.Price = increase ? price + value : price - value;

                    portfolioDemoChannelFeederMessage.Time = DateTime.UtcNow.TimeOfDay;

                    await EmitMessageAsync(portfolioDemoChannelFeederMessage, _cancellationToken);
                }
            }
        }

        // Issue #39: LoggerMessage-generated methods for this channel's log call sites. EventIds
        // 2201-2203 are this file's own block; no cross-file EventId registry exists yet in this repo.
        private static partial class Log
        {
            /// <summary>Logs that the background simulation loop faulted and stopped running.</summary>
            [LoggerMessage(EventId = 2201, Level = LogLevel.Error, Message = "PortfolioDemoChannel's background simulation loop faulted and stopped running.")]
            public static partial void SimulationLoopFaulted(ILogger logger, Exception? exception);

            /// <summary>Logs that handling a new subscription for a key failed.</summary>
            [LoggerMessage(EventId = 2202, Level = LogLevel.Error, Message = "Failed to handle new subscription for key {Key} on channel {ChannelName}.")]
            public static partial void SubscriptionAddedFailed(ILogger logger, Exception? exception, string? key, string channelName);

            /// <summary>Logs that handling a subscription removal for a key failed.</summary>
            [LoggerMessage(EventId = 2203, Level = LogLevel.Error, Message = "Failed to handle subscription removal for key {Key} on channel {ChannelName}.")]
            public static partial void SubscriptionRemovedFailed(ILogger logger, Exception? exception, string? key, string channelName);
        }
    }
}
