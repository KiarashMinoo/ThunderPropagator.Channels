using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Demo.StockListBasic.Channel;
using ThunderPropagator.Channels.Demo.StockListBasic.Configuration;
using ThunderPropagator.Channels.Demo.StockListBasic.Feeders;
using ThunderPropagator.Channels.Demo.StockListBasic.Messages;

namespace ThunderPropagator.Channels.Demo.StockListBasic.Extensions
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
                .AddChannelFeeder<StockListBasicDemoChannel, StockListBasicDemoChannelFeeder, StockListBasicDemoChannelFeederMessage, StockListBasicDemoChannelFeederConfiguration>(configuration =>
                    configuration.Bind(stockListBasicDemoChannelConfiguration.FeederConfiguration));

            return services;
        }
    }
}