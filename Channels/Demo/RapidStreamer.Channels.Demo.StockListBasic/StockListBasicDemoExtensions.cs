using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Demo.StockListBasic
{
    public static class StockListBasicDemoExtensions
    {
        public static IServiceCollection AddStockListBasicDemoChannel(this IServiceCollection services, Action<StockListBasicDemoChannelConfiguration>? channelConfigurator = null)
        {
            StockListBasicDemoChannelConfiguration stockListBasicDemoChannelConfiguration = new();
            channelConfigurator?.Invoke(stockListBasicDemoChannelConfiguration);

            services
                .AddSingleton(stockListBasicDemoChannelConfiguration)
                .AddChannel<StockListBasicDemoChannel>()
                .AddChannelFeeder<StockListBasicDemoChannel, StockListBasicDemoChannelFeeder, StockListBasicDemoChannelFeederMessage, StockListBasicDemoChannelFeederConfiguration>();

            return services;
        }
    }
}