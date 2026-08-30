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
        class PortfolioDemoChannel : AbstractChannel<PortfolioDemoChannelMetadata, PortfolioDemoChannelConfiguration>
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
                task => _logger.LogError(task.Exception, "PortfolioDemoChannel's background simulation loop faulted and stopped running."),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        internal static decimal GeneratePrice() => new Faker().Random.Decimal(1M, 100M);

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
                task => _logger.LogError(task.Exception, "Failed to handle new subscription for key {Key} on channel {ChannelName}.", key, Metadata.ChannelName),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        protected override void OnSubscriptionRemoved(Subscription subscription)
        {
            base.OnSubscriptionRemoved(subscription);

            var key = subscription.SubscribedPrograms.SubscribedKeys[nameof(PortfolioDemoChannelFeederMessage.Key)];

            _ = HandleSubscriptionRemovedAsync(key).ContinueWith(
                task => _logger.LogError(task.Exception, "Failed to handle subscription removal for key {Key} on channel {ChannelName}.", key, Metadata.ChannelName),
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
    }
}
