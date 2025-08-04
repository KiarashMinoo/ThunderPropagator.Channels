using System.Runtime.CompilerServices;
using Bogus;
using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Demo.StockListBasic
{
    internal
#if !DEBUG
        sealed
#endif
        class StockListBasicDemoChannelFeeder : IterativeFeeder<StockListBasicDemoChannel, StockListBasicDemoChannelFeederMessage, StockListBasicDemoChannelFeederConfiguration>
    {
        private readonly HashSet<StockListBasicDemoChannelFeederMessage> _stocks;

        public StockListBasicDemoChannelFeeder(StockListBasicDemoChannel channel,
            StockListBasicDemoChannelFeederConfiguration feederConfiguration,
            IFeederHandler<StockListBasicDemoChannel, StockListBasicDemoChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            _stocks = new Faker<StockListBasicDemoChannelFeederMessage>()
                .RuleFor(x => x.Key, StockListBasicDemoChannelMetadata.StockListBasicDemo)
                .RuleFor(x => x.Stock, faker => faker.Company.CompanyName())
                .RuleFor(x => x.ReferencePrice, faker => faker.Random.Decimal(100M))
                .RuleFor(x => x.Time, DateTime.UtcNow.TimeOfDay)
                .Generate(30)
                .ToHashSet();
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<StockListBasicDemoChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(500, 90_000)), cancellationToken);

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