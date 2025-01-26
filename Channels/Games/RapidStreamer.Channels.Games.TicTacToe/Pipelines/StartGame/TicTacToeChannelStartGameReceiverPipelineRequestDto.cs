using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;

namespace RapidStreamer.Channels.Games.TicTacToe.Pipelines.StartGame
{
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelStartGameReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required string SessionId
        {
            get => (string)this[nameof(SessionId)];
            set => this[nameof(SessionId)] = value;
        }

        public required string PlayerName
        {
            get => (string)this[nameof(PlayerName)];
            set => this[nameof(PlayerName)] = value;
        }
    }
}