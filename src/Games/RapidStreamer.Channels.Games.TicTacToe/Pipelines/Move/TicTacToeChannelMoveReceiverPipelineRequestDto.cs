using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;

namespace RapidStreamer.Channels.Games.TicTacToe.Pipelines.Move
{
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelMoveReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required string SessionId
        {
            get => (string)this[nameof(SessionId)];
            set => this[nameof(SessionId)] = value;
        }

        public required int Row
        {
            get => (int)this[nameof(Row)];
            set => this[nameof(Row)] = value;
        }

        public required int Column
        {
            get => (int)this[nameof(Column)];
            set => this[nameof(Column)] = value;
        }
    }
}