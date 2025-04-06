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
using RapidStreamer.Channels.Demo.Portfolio.Dtos;
using RapidStreamer.Infrastructure.Channels;

namespace RapidStreamer.Channels.Demo.Portfolio.Pipelines
{
    [ReceivePipelineRequestSchema(typeof(PortfolioDemoChannelReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(PortfolioDemoChannelReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelBuyReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<PortfolioDemoChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => "Buy";

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            var activityName = $"{channelInfo.ChannelName}_{GetType().GetTypeInfo().Name}_{nameof(Invoke)}";
            _counter ??= Telemetry.CreateCounter<long>(activityName.ToSnakeCase());

            using var activity = Telemetry.StartActivity(activityName, ActivityKind.Consumer)?
                .SetTag(nameof(ChannelInfo.ChannelType), channelInfo.ChannelType)
                .SetTag(nameof(ChannelInfo.ChannelKey), channelInfo.ChannelKey)
                .SetTag(nameof(ChannelInfo.ChannelName), channelInfo.ChannelName);

            try
            {
                var isBuy = context.Request.RouteTable["RequestType"].Equals(RequestKey);
                if (isBuy)
                {
                    var portfolioRequest = context.Request.GetRequestContentFormData<PortfolioDemoChannelReceiverPipelineRequestDto>()!;
                    if (!portfolioRequest.IsBuy)
                    {
                        Logger.LogWarning("The request content form is not buy");
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

                    if (snapshotEntries.Length != 0)
                    {
                        foreach (var snapshotEntry in snapshotEntries)
                        {
                            PortfolioDemoChannelFeederMessage portfolioDemoChannelFeederMessage = new(snapshotEntry.Snapshot);

                            portfolioDemoChannelFeederMessage.Quantity += portfolioRequest.Quantity;

                            channelInfo.Channel.EmitMessage(portfolioDemoChannelFeederMessage);
                        }
                    }
                    else
                    {
                        PortfolioDemoChannelFeederMessage portfolioDemoChannelFeederMessage = new()
                        {
                            Key = portfolioRequest.Key,
                            Stock = portfolioRequest.Stock,
                            Price = PortfolioDemoChannel.GeneratePrice(),
                            Quantity = portfolioRequest.Quantity,
                            Time = DateTime.UtcNow.TimeOfDay
                        };

                        channelInfo.Channel.EmitMessage(portfolioDemoChannelFeederMessage);
                    }

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new PortfolioDemoChannelReceiverPipelineResponseDto
                    {
                        Echo = "Bought \ud83d\udc4d"
                    };

                    _counter?.Add(1, new KeyValuePair<string, object?>(nameof(channelInfo.ChannelName), channelInfo.ChannelName));
                }
                else
                {
                    await next(context, cancellationToken);
                }
            }
            finally
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
        }
    }
}