using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using CaseConverter;
using Microsoft.Extensions.Logging;
using RapidStreamer.Application.Channels.Contexts;
using RapidStreamer.Application.Pipelines.Receivers;
using RapidStreamer.Application.Pipelines.Receivers.Attributes;
using RapidStreamer.BuildingBlocks.Application;
using RapidStreamer.BuildingBlocks.Application.Helpers;
using RapidStreamer.Channels.Demo.Portfolio.Dtos;
using RapidStreamer.Infrastructure.Channels;

namespace RapidStreamer.Channels.Demo.Portfolio.Pipelines
{
    [ReceivePipelineRequestSchema(typeof(PortfolioRequestDto))]
    [ReceivePipelineResponseSchema(typeof(PortfolioResponseDto))]
    public
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelSellPipeline : AbstractReceivePipeline<PortfolioDemoChannel>
    {
        private Counter<long>? _counter;

        public override string RequestKey => "Sell";

        public PortfolioDemoChannelSellPipeline(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            var activityName = $"{channelInfo.ChannelName}_{GetType().GetTypeInfo().Name}_{nameof(Invoke)}";
            if (_counter == null)
                _counter = Telemetry.CreateCounter<long>(activityName.ToSnakeCase());

#if DEBUG
            using var activity = Telemetry.StartActivity(activityName, ActivityKind.Consumer)?
                .SetTag(nameof(ChannelInfo.ChannelType), channelInfo.ChannelType).SetTag(nameof(ChannelInfo.ChannelKey), channelInfo.ChannelKey).SetTag(nameof(ChannelInfo.ChannelName), channelInfo.ChannelName);
#endif

            try
            {
                var isSell = context.Request.RouteTable["RequestType"].Equals(RequestKey);
                if (isSell)
                {
                    var portfolioRequest = context.Request.GetRequestContentFormData<PortfolioRequestDto>()!;
                    if (portfolioRequest.IsBuy)
                    {
                        Logger.LogWarning("The request content form is not sell");
                        return;
                    }

                    if (portfolioRequest.Quantity <= 0)
                    {
                        Logger.LogWarning("The quantity of portfolio request is zero or negative.");
                        return;
                    }

                    var snapshotEntries = await channelInfo.Channel
                        .SearchSnapshotsAsync(snapshotEntry => snapshotEntry.Keys[nameof(PortfolioDemoChannelFeederMessage.Key)]?.Equals(portfolioRequest.Key) == true &&
                                                               snapshotEntry.Keys[nameof(PortfolioDemoChannelFeederMessage.Stock)]?.Equals(portfolioRequest.Stock) == true,
                            0,
                            0,
                            cancellationToken);

                    foreach (var snapshotEntry in snapshotEntries)
                    {
                        PortfolioDemoChannelFeederMessage portfolioDemoChannelFeederMessage = new(snapshotEntry.Snapshot);

                        portfolioDemoChannelFeederMessage.Quantity -= portfolioRequest.Quantity;
                        if (portfolioDemoChannelFeederMessage.Quantity <= 0)
                            portfolioDemoChannelFeederMessage.IsDeleted = true;

                        channelInfo.Channel.EmitMessage(portfolioDemoChannelFeederMessage);
                    }

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new PortfolioResponseDto
                    {
                        Echo = "Sold \ud83d\udc4d"
                    };

                    _counter.Add(1, new KeyValuePair<string, object?>(nameof(channelInfo.ChannelName), channelInfo.ChannelName));
                }
                else
                {
                    await next(context, cancellationToken);
                }
            }
            finally
            {
#if DEBUG
                activity?.SetStatus(ActivityStatusCode.Ok);
#endif
            }
        }
    }
}