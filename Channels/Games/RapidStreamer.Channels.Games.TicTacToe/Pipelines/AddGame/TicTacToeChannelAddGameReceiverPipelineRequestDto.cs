using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;
using RapidStreamer.Channels.Games.TicTacToe.Game.Enums;

namespace RapidStreamer.Channels.Games.TicTacToe.Pipelines.AddGame
{
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelAddGameReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
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

        public PlayerSign Sign
        {
            get => (PlayerSign)this[nameof(Sign)];
            set => this[nameof(Sign)] = value;
        }

        public PlayerKind OpponentKind
        {
            get => (PlayerKind)this[nameof(OpponentKind)];
            set => this[nameof(OpponentKind)] = value;
        }

        public DifficultyLevel DifficultyLevel
        {
            get => (DifficultyLevel)this[nameof(DifficultyLevel)];
            set => this[nameof(DifficultyLevel)] = value;
        }
    }
}