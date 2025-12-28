using ThunderPropagator.Application.Collections;

namespace ThunderPropagator.Channels.Games.TicTacToe.Pipelines.Move
{
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelMoveReceiverPipelineResponseDto : ResponseContentFormCollection
    {
    }
}