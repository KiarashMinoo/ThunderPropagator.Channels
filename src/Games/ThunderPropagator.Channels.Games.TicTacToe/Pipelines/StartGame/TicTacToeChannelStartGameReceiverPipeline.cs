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
using ThunderPropagator.Channels.Games.TicTacToe.Channel;

namespace ThunderPropagator.Channels.Games.TicTacToe.Pipelines.StartGame
{
    [ReceivePipelineRequestSchema(typeof(TicTacToeChannelStartGameReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(TicTacToeChannelStartGameReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelStartGameReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<TicTacToeChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => nameof(StartGame);

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
                var startGame = context.Request.RouteTable["RequestType"].Equals(RequestKey);
                if (startGame)
                {
                    var channel = (TicTacToeChannel)channelInfo.Channel;
                    var StartGameRequest = context.Request.GetRequestContentFormData<TicTacToeChannelStartGameReceiverPipelineRequestDto>()!;
                    var subscription = await channel.StartGameAsync(context.WebSocketConnectionInfo,
                        context.Request.RequestId,
                        StartGameRequest.SessionId,
                        StartGameRequest.PlayerName,
                        cancellationToken);

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new TicTacToeChannelStartGameReceiverPipelineResponseDto
                    {
                        Subscription = subscription
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