using System.Runtime.CompilerServices;
using Bogus;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.StockListBasic.Channel;
using ThunderPropagator.Channels.Demo.StockListBasic.Feeders;
using ThunderPropagator.Channels.Demo.StockListBasic.Messages;
using ThunderPropagator.Channels.Demo.StockListBasic.Metadata;

namespace ThunderPropagator.Channels.Demo.StockListBasic.Feeders
{
    internal
#if !DEBUG
        sealed
#endif
        class StockListBasicDemoChannelFeeder : IterativeFeeder<StockListBasicDemoChannel, StockListBasicDemoChannelFeederMessage, StockListBasicDemoChannelFeederConfiguration>
    {
        private readonly HashSet<StockListBasicDemoChannelFeederMessage> _stocks;

        // Tracks active subscriptions locally via the channel's public SubscriptionAdded/Removed
        // events, since neither is exposed to feeder code any other way. Read with Volatile.Read
        // and written with Interlocked so the poll loop always sees the latest count.
        private int _activeSubscriptions;

        private readonly StockListBasicDemoChannelFeederConfiguration _feederConfiguration;

        public StockListBasicDemoChannelFeeder(StockListBasicDemoChannel channel,
            StockListBasicDemoChannelFeederConfiguration feederConfiguration,
            IFeederHandler<StockListBasicDemoChannel, StockListBasicDemoChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            _feederConfiguration = feederConfiguration;

            _stocks = new Faker<StockListBasicDemoChannelFeederMessage>()
                .RuleFor(x => x.Key, StockListBasicDemoChannelMetadata.StockListBasicDemo)
                .RuleFor(x => x.Stock, faker => faker.Company.CompanyName())
                .RuleFor(x => x.ReferencePrice, faker => faker.Random.Decimal(100M))
                .RuleFor(x => x.Time, DateTime.UtcNow.TimeOfDay)
                .Generate(30)
                .ToHashSet();

            channel.SubscriptionAdded += (_, _) => Interlocked.Increment(ref _activeSubscriptions);
            channel.SubscriptionRemoved += (_, _) => Interlocked.Decrement(ref _activeSubscriptions);
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<StockListBasicDemoChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var minMilliseconds = (int)_feederConfiguration.MinPollInterval.TotalMilliseconds;
            var maxMilliseconds = (int)_feederConfiguration.MaxPollInterval.TotalMilliseconds;
            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(minMilliseconds, maxMilliseconds)), cancellationToken);

            if (Volatile.Read(ref _activeSubscriptions) <= 0)
                yield break;

            var stocks = _stocks
                .OrderBy(_ => Guid.NewGuid())
                .Take(Random.Shared.Next(1, _stocks.Count))
                .ToList();

            var faker = new Faker();
            foreach (var stock in stocks)
            {
                var flag = faker.Random.Bool();
                var trade = faker.Random.Bool();

                stock.OrderPrice = flag
                    ? faker.Random.Decimal(stock.ReferencePrice, stock.UpperPrice)
                    : faker.Random.Decimal(stock.LowerPrice, stock.ReferencePrice);

                stock.Quantity = faker.Random.Int(1, 100);

                if (trade)
                {
                    if (stock.OpeningPrice == 0)
                        stock.OpeningPrice = stock.OrderPrice;

                    stock.TradePrice = stock.OrderPrice;

                    if (stock.LastPrice == 0)
                    {
                        stock.Change = stock.TradePrice - stock.ReferencePrice;
                        if (stock.ReferencePrice != 0)
                            stock.ChangePercent = stock.Change / stock.ReferencePrice;
                    }
                    else
                    {
                        stock.Change = stock.TradePrice - stock.LastPrice;
                        if (stock.LastPrice != 0)
                            stock.ChangePercent = stock.Change / stock.LastPrice;
                    }

                    stock.LastPrice = stock.TradePrice;
                }

                stock.Time = DateTime.UtcNow.TimeOfDay;

                yield return stock;
            }
        }
    }
}