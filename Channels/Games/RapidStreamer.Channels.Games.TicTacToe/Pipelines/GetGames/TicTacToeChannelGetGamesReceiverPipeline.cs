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

namespace RapidStreamer.Channels.Games.TicTacToe.Pipelines.GetGames
{
    [ReceivePipelineResponseSchema(typeof(TicTacToeChannelGetGamesReceiverPipelineResponseDto))]
    public
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelGetGamesReceiverPipeline : AbstractReceivePipeline<TicTacToeChannel>
    {
        private Counter<long>? _counter;

        public override string RequestKey => "GetGames";

        public TicTacToeChannelGetGamesReceiverPipeline(ILoggerFactory loggerFactory) : base(loggerFactory)
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
                var addGame = context.Request.RouteTable["RequestType"].Equals(RequestKey);
                if (addGame)
                {
                    var channel = (TicTacToeChannel)channelInfo.Channel;

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new TicTacToeChannelGetGamesReceiverPipelineResponseDto
                    {
                        Items = channel.GetGames().Select(game => new GetGamesItemResponseDto(game.SessionId, game.PlayerName))
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