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
using RapidStreamer.Infrastructure.Channels;

namespace RapidStreamer.Channels.Games.TicTacToe.Pipelines.Move
{
    [ReceivePipelineRequestSchema(typeof(TicTacToeChannelMoveReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(TicTacToeChannelMoveReceiverPipelineResponseDto))]
    public
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelMoveReceiverPipeline : AbstractReceivePipeline<TicTacToeChannel>
    {
        private Counter<long>? _counter;

        public override string RequestKey => "Move";

        public TicTacToeChannelMoveReceiverPipeline(ILoggerFactory loggerFactory) : base(loggerFactory)
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
                var move = context.Request.RouteTable["RequestType"].Equals(RequestKey);
                if (move)
                {
                    var channel = (TicTacToeChannel)channelInfo.Channel;
                    var moveRequest = context.Request.GetRequestContentFormData<TicTacToeChannelMoveReceiverPipelineRequestDto>()!;
                    channel.Move(moveRequest.SessionId,
                        context.WebSocketConnectionInfo,
                        moveRequest.Row,
                        moveRequest.Column);

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new TicTacToeChannelMoveReceiverPipelineResponseDto();

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