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

namespace ThunderPropagator.Channels.Demo.Quiz.Pipelines.Start
{
    /// <summary>
    /// Wire-facing entry point for a host starting their lobby early (request key <c>Quiz/Start</c>) —
    /// a thin wrapper around <see cref="QuizChannel.StartGame"/>, which owns every actual rule (host
    /// authorization, phase/player-count prerequisites, and the transition itself). The outcome is
    /// returned as this request's own response, which the framework delivers only to the requesting
    /// connection; the actual phase-transition broadcast <see cref="QuizChannel.StartGame"/> emits
    /// reaches every other subscriber through the channel's ordinary broadcast path, not this response.
    /// </summary>
    [ReceivePipelineRequestSchema(typeof(QuizStartGameReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(QuizStartGameReceiverPipelineResponseDto))]
    public
#if !DEBUG
        sealed
#endif
        class QuizStartGameReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<QuizChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => "Quiz/Start";

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
                    var request = context.Request.GetRequestContentFormData<QuizStartGameReceiverPipelineRequestDto>()!;

                    var channel = (QuizChannel)channelInfo.Channel;
                    var outcome = channel.StartGame(context.WebSocketConnectionInfo, request.GameId);

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new QuizStartGameReceiverPipelineResponseDto { Outcome = outcome };

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
