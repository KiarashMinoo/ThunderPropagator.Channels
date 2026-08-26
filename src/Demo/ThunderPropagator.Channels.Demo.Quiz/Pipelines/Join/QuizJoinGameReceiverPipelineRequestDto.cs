using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;
using ThunderPropagator.Channels.Demo.Quiz.Channel;

namespace ThunderPropagator.Channels.Demo.Quiz.Pipelines.Join
{
    public
#if !DEBUG
        sealed
#endif
        class QuizJoinGameReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        /// <summary>The session to join. Must already exist — see <see cref="QuizGameNotFoundException"/>.</summary>
        public required string GameId
        {
            get => (string)this[nameof(GameId)];
            set => this[nameof(GameId)] = value;
        }

        /// <summary>The display name to join under — normalized (whitespace trimmed/collapsed) before use; see <see cref="QuizChannel"/>'s own remarks.</summary>
        public required string PlayerName
        {
            get => (string)this[nameof(PlayerName)];
            set => this[nameof(PlayerName)] = value;
        }
    }
}
