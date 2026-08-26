using System.Collections.Concurrent;
using ThunderPropagator.Channels.Demo.Quiz.Channel;
using ThunderPropagator.Channels.Demo.Quiz.Feeders;

namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// How <see cref="QuizChannel"/> reaches the <see cref="QuizGameLoop"/> actually driving a given
    /// GameId, even though the loop instance itself is owned and constructed by <see cref="QuizFeeder"/>
    /// — a separate object <see cref="QuizChannel"/> has no reference to (a feeder holds a reference to
    /// its channel, never the reverse). <see cref="QuizFeeder"/> registers its loop here at construction;
    /// <see cref="QuizChannel.SubmitAnswer"/> (#192) is this type's only real consumer, needing the
    /// active loop to actually score an answer rather than merely touch session/membership state the
    /// way <see cref="QuizChannel.Join"/> (#191) does via <see cref="QuizGameSessionStore"/> alone. Kept
    /// as its own singleton, separate from <see cref="QuizGameSessionStore"/>, so that store stays
    /// exactly what #187 scoped it as — pure session/membership state with no knowledge of scoring or
    /// timing.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizGameLoopRegistry
    {
        private readonly ConcurrentDictionary<string, QuizGameLoop> _loopsByGameId = new(StringComparer.Ordinal);

        public void Register(string gameId, QuizGameLoop gameLoop)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
            ArgumentNullException.ThrowIfNull(gameLoop);

            _loopsByGameId[gameId] = gameLoop;
        }

        public QuizGameLoop? TryGet(string gameId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameId);

            return _loopsByGameId.GetValueOrDefault(gameId);
        }
    }
}
