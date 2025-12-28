using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Collections;

namespace ThunderPropagator.Channels.Games.TicTacToe.Pipelines.AddGame
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