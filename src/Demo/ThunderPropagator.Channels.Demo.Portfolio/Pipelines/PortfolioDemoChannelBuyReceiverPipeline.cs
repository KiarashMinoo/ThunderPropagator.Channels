using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
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
        partial class PortfolioDemoChannelBuyReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<PortfolioDemoChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => "Buy";

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
                var isBuy = context.Request.RouteTable["RequestType"].Equals(RequestKey);
                if (isBuy)
                {
                    var portfolioRequest = context.Request.GetRequestContentFormData<PortfolioDemoChannelReceiverPipelineRequestDto>()!;
                    if (!portfolioRequest.IsBuy)
                    {
                        Log.RequestFormNotBuy(Logger);
                        return;
                    }

                    if (portfolioRequest.Quantity <= 0)
                    {
                        Log.QuantityNotPositive(Logger);
                        return;
                    }

                    // Issue #36: Key is resolved from the caller's own subscription rather than
                    // trusted from the request body, so a connection can only ever buy against the
                    // portfolio position it subscribed to create, never another subscriber's.
                    var key = ((PortfolioDemoChannel)channelInfo.Channel).FindSubscribedKey(context.WebSocketConnectionInfo.ConnectionId);
                    if (key is null)
                    {
                        Log.RejectedBuyNoSubscription(Logger);
                        return;
                    }

                    var snapshotEntries = await channelInfo.Channel
                        .SearchSnapshotsAsync(snapshotEntry => snapshotEntry.Keys[nameof(PortfolioDemoChannelFeederMessage.Key)]?.Equals(key) == true &&
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
                            Key = key,
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

        // Issue #39: LoggerMessage-generated methods for this pipeline's log call sites. EventIds
        // 2001-2003 are this file's own block; no cross-file EventId registry exists yet in this repo.
        private static partial class Log
        {
            /// <summary>Logs that a request routed to the Buy pipeline was not itself a buy request.</summary>
            [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "The request content form is not buy")]
            public static partial void RequestFormNotBuy(ILogger logger);

            /// <summary>Logs that a buy request's quantity was zero or negative.</summary>
            [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "The quantity of portfolio request is zero or negative.")]
            public static partial void QuantityNotPositive(ILogger logger);

            /// <summary>Logs that a buy request was rejected because its connection has no active portfolio subscription.</summary>
            [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Rejected a buy request from a connection with no active portfolio subscription.")]
            public static partial void RejectedBuyNoSubscription(ILogger logger);
        }
    }
}