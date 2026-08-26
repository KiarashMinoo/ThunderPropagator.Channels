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

namespace ThunderPropagator.Channels.Demo.Quiz.Pipelines.Answer
{
    /// <summary>
    /// Wire-facing entry point for submitting an answer (request key <c>Quiz/Answer</c>) — a thin
    /// wrapper around <see cref="QuizChannel.SubmitAnswer"/>, which owns every actual rule (identity
    /// resolution, phase/staleness/option-index validation, duplicate policy, scoring). The
    /// acknowledgement is returned as this request's own response, which the framework delivers only
    /// to the requesting connection — the unicast #192's own AC asks for.
    /// </summary>
    [ReceivePipelineRequestSchema(typeof(QuizSubmitAnswerReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(QuizSubmitAnswerReceiverPipelineResponseDto))]
    public
#if !DEBUG
        sealed
#endif
        class QuizSubmitAnswerReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<QuizChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => "Quiz/Answer";

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
                    var request = context.Request.GetRequestContentFormData<QuizSubmitAnswerReceiverPipelineRequestDto>()!;

                    var channel = (QuizChannel)channelInfo.Channel;
                    var outcome = channel.SubmitAnswer(context.WebSocketConnectionInfo, request.GameId, request.QuestionIndex, request.OptionIndex);

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new QuizSubmitAnswerReceiverPipelineResponseDto { Outcome = outcome };

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
