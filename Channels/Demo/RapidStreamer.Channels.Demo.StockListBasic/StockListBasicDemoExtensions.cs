using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Demo.StockListBasic
{
    public static class StockListBasicDemoExtensions
    {
        public static IServiceCollection AddStockListBasicDemoChannel(this IServiceCollection services)
        {
            services.AddChannel<StockListBasicDemoChannel>()
                .AddChannelFeeder<StockListBasicDemoChannel, StockListBasicDemoChannelFeeder, StockListBasicDemoChannelFeederMessage, StockListBasicDemoChannelFeederConfiguration>();

            return services;
        }
    }
}