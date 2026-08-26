using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Demo.Portfolio.Pipelines;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Demo.Portfolio.Channel;
using ThunderPropagator.Channels.Demo.Portfolio.Configuration;

namespace ThunderPropagator.Channels.Demo.Portfolio.Extensions
{
    public static class PortfolioDemoExtensions
    {
        public static IServiceCollection AddPortfolioDemoChannel(this IServiceCollection services, Action<PortfolioDemoChannelConfiguration>? channelConfigurator = null)
        {
            PortfolioDemoChannelConfiguration portfolioDemoChannelConfiguration = new();
            channelConfigurator?.Invoke(portfolioDemoChannelConfiguration);
            
            services
                .AddSingleton(portfolioDemoChannelConfiguration)
                .AddChannel<PortfolioDemoChannel>()
                .AddReceivePipeline<PortfolioDemoChannel, PortfolioDemoChannelBuyReceiverPipeline>()
                .AddReceivePipeline<PortfolioDemoChannel, PortfolioDemoChannelSellReceiverPipeline>();

            return services;
        }
    }
}