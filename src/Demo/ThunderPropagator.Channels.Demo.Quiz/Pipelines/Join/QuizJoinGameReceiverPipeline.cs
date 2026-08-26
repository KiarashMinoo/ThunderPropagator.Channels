using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Demo.Quiz.Channel;

namespace ThunderPropagator.Channels.Demo.Quiz.Pipelines.Join
{
    /// <summary>
    /// Wire-facing entry point for joining a quiz lobby (request key <c>Quiz/Join</c>) — a thin wrapper
    /// around <see cref="QuizChannel.Join"/>, which owns every actual business rule (missing/full
    /// games, non-lobby joins, duplicate names/reconnects). The join result is returned as this
    /// request's own response, which the framework delivers only to the requesting connection — the
    /// unicast #191's own AC asks for.
    /// </summary>
    [ReceivePipelineRequestSchema(typeof(QuizJoinGameReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(QuizJoinGameReceiverPipelineResponseDto))]
    public
#if !DEBUG
        sealed
#endif
        class QuizJoinGameReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<QuizChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => "Quiz/Join";

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
                if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
                {
                    var request = context.Request.GetRequestContentFormData<QuizJoinGameReceiverPipelineRequestDto>()!;

                    var channel = (QuizChannel)channelInfo.Channel;
                    var joinResult = channel.Join(context.WebSocketConnectionInfo, context.Request.RequestId, request.GameId, request.PlayerName);

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new QuizJoinGameReceiverPipelineResponseDto
                    {
                        Subscription = joinResult.Subscription,
                        IsReconnect = joinResult.IsReconnect,
                        IsHost = joinResult.IsHost,
                        PlayerName = joinResult.PlayerName
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
