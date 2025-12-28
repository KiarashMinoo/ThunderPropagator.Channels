using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Collections;

namespace ThunderPropagator.Channels.Games.TicTacToe.Pipelines.StartGame
{
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelStartGameReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required Subscription Subscription { get; init; }
    }
}