using RapidStreamer.Application.Channels.Subscribers;
using RapidStreamer.Application.Collections;

namespace RapidStreamer.Channels.Games.TicTacToe.Pipelines.AddGame
{
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelAddGameReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required Subscription Subscription { get; init; }
    }
}