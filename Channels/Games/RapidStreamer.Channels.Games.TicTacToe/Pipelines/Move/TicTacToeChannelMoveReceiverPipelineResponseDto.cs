using RapidStreamer.Application.Collections;

namespace RapidStreamer.Channels.Games.TicTacToe.Pipelines.Move
{
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelMoveReceiverPipelineResponseDto : ResponseContentFormCollection
    {
    }
}