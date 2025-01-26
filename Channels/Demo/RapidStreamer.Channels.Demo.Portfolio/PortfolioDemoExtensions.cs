using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Channels.Demo.Portfolio.Pipelines;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Demo.Portfolio
{
    public static class PortfolioDemoExtensions
    {
        public static IServiceCollection AddPortfolioDemoChannel(this IServiceCollection services)
        {
            services.AddChannel<PortfolioDemoChannel>()
                .AddReceivePipeline<PortfolioDemoChannel, PortfolioDemoChannelBuyReceiverPipeline>()
                .AddReceivePipeline<PortfolioDemoChannel, PortfolioDemoChannelSellReceiverPipeline>();

            return services;
        }
    }
}