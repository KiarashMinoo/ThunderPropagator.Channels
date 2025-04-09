using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RapidStreamer.Application.Channels;
using RapidStreamer.Application.Channels.Exceptions;
using RapidStreamer.Application.Channels.Subscribers;
using RapidStreamer.BuildingBlocks.Application;
using RapidStreamer.BuildingBlocks.Application.Enums;

namespace RapidStreamer.Channels.Demo.Portfolio
{
    public
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannel : AbstractChannel<PortfolioDemoChannelMetadata>
    {
        public const string PortfolioDemo = nameof(PortfolioDemo);
        public const string PortfolioDemoItems = nameof(PortfolioDemoItems);

        private readonly CancellationToken _cancellationToken;

        public PortfolioDemoChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _cancellationToken = serviceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

            new Thread(Simulate).Start();
        }

        internal static decimal GeneratePrice() => new Faker().Random.Decimal(1M, 100M);

        protected override void OnSubscriptionAdded(Subscription subscription)
        {
            base.OnSubscriptionAdded(subscription);

            var key = subscription.SubscribedPrograms.SubscribedKeys[nameof(PortfolioDemoChannelFeederMessage.Key)];

            var snapshotEntries = SearchSnapshotsAsync(snapshotEntry => snapshotEntry.Snapshot.ContainsKey(key), 0, 0, _cancellationToken)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

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
                EmitMessage(portfolioDemoChannelFeederMessage);
        }

        protected override void OnSubscriptionRemoved(Subscription subscription)
        {
            base.OnSubscriptionRemoved(subscription);

            var key = subscription.SubscribedPrograms.SubscribedKeys[nameof(PortfolioDemoChannelFeederMessage.Key)];

            var snapshotEntries = SearchSnapshotsAsync(snapshotEntry => snapshotEntry.Snapshot.ContainsKey(key), 0, 0, _cancellationToken)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            if (snapshotEntries.Length > 0)
            {
                foreach (var snapshotEntry in snapshotEntries)
                {
                    PortfolioDemoChannelFeederMessage portfolioDemoChannelFeederMessage = new(snapshotEntry.Snapshot);
                    portfolioDemoChannelFeederMessage.IsDeleted = true;
                    EmitMessage(snapshotEntry.HashKey, CastType.Unicast, portfolioDemoChannelFeederMessage, typeof(PortfolioDemoChannelFeederMessage));
                }
            }
        }

        private async void Simulate()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(500, 90_000)), _cancellationToken);

                var snapshotEntries = await SearchSnapshotsAsync(_ => true, 0, 0, _cancellationToken);

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

                    EmitMessage(portfolioDemoChannelFeederMessage);
                }
            }
        }
    }
}