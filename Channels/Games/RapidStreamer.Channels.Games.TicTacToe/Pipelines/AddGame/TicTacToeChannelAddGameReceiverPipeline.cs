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

namespace RapidStreamer.Channels.Games.TicTacToe.Pipelines.AddGame
{
    [ReceivePipelineRequestSchema(typeof(TicTacToeChannelAddGameReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(TicTacToeChannelAddGameReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelAddGameReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<TicTacToeChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => nameof(AddGame);

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
                var addGame = context.Request.RouteTable["RequestType"].Equals(RequestKey);
                if (addGame)
                {
                    var channel = (TicTacToeChannel)channelInfo.Channel;
                    var addGameRequest = context.Request.GetRequestContentFormData<TicTacToeChannelAddGameReceiverPipelineRequestDto>()!;
                    var subscription = channel.AddGame(context.WebSocketConnectionInfo,
                        context.Request.RequestId,
                        addGameRequest.SessionId,
                        addGameRequest.PlayerName,
                        addGameRequest.Sign,
                        addGameRequest.OpponentKind,
                        addGameRequest.DifficultyLevel);

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new TicTacToeChannelAddGameReceiverPipelineResponseDto
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