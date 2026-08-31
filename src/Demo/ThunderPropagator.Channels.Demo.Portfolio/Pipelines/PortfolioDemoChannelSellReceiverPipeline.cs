using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.BuildingBlocks.Application.Helpers;
using ThunderPropagator.Channels.Demo.Portfolio.Pipelines.Dtos;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Demo.Portfolio.Channel;
using ThunderPropagator.Channels.Demo.Portfolio.Messages;

namespace ThunderPropagator.Channels.Demo.Portfolio.Pipelines
{
    [ReceivePipelineRequestSchema(typeof(PortfolioDemoChannelReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(PortfolioDemoChannelReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelSellReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<PortfolioDemoChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => "Sell";

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            var activityName = $"{channelInfo.ChannelName}_{GetType().GetTypeInfo().Name}_{nameof(Invoke)}";
            _counter ??= Telemetry.CreateCounter<long>($"thunderpropagator.{activityName.ToLowerInvariant().Replace('_', '.')}");

            using var activity = Telemetry.StartActivity(activityName, ActivityKind.Consumer)?
                .SetTag(nameof(ChannelInfo.ChannelType), channelInfo.ChannelType)
                .SetTag(nameof(ChannelInfo.ChannelKey), channelInfo.ChannelKey)
                .SetTag(nameof(ChannelInfo.ChannelName), channelInfo.ChannelName);

            try
            {
                var isSell = context.Request.RouteTable["RequestType"].Equals(RequestKey);
                if (isSell)
                {
                    var portfolioRequest = context.Request.GetRequestContentFormData<PortfolioDemoChannelReceiverPipelineRequestDto>()!;
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

                    // Issue #36: Key is resolved from the caller's own subscription rather than
                    // trusted from the request body, so a connection can only ever sell from the
                    // portfolio position it subscribed to create, never another subscriber's.
                    var key = ((PortfolioDemoChannel)channelInfo.Channel).FindSubscribedKey(context.WebSocketConnectionInfo.ConnectionId);
                    if (key is null)
                    {
                        Logger.LogWarning("Rejected a sell request from a connection with no active portfolio subscription.");
                        return;
                    }

                    var snapshotEntries = await channelInfo.Channel
                        .SearchSnapshotsAsync(snapshotEntry => snapshotEntry.Keys[nameof(PortfolioDemoChannelFeederMessage.Key)]?.Equals(key) == true &&
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
                    context.Response.ResponseContent = new PortfolioDemoChannelReceiverPipelineResponseDto
                    {
                        Echo = "Sold \ud83d\udc4d"
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