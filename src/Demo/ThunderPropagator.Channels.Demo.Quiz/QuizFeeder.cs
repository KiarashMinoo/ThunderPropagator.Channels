using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Game;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Drives the demo's single, perpetually-running quiz game via <see cref="QuizGameLoop"/> — an
    /// <see cref="IterativeFeeder{TChannel,TFeederMessage,TFeederConfiguration}"/> rather than a raw
    /// thread (#189's own AC), since #191/#192/#193 (Join/Answer/host-authorized Start) don't exist
    /// yet to create real player-driven sessions. One fixed <see cref="DemoGameId"/> is enough for
    /// that: it is still the same <see cref="QuizGameSessionStore"/> a future ticket's Join pipeline
    /// would resolve real sessions through, so nothing here needs to change once those tickets add
    /// more of them.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizFeeder : IterativeFeeder<QuizChannel, QuizChannelFeederMessage, QuizFeederConfiguration>
    {
        internal const string DemoGameId = "demo";

        // Tracks active subscriptions locally via the channel's public SubscriptionAdded/Removed
        // events, since neither is exposed to feeder code any other way — mirrors every other
        // IterativeFeeder in this codebase (e.g. NowClockFeeder). Read with Volatile.Read and written
        // with Interlocked so the loop always sees the latest count.
        private int _activeSubscriptions;

        private readonly QuizGameLoop _gameLoop;

        public QuizFeeder(
            QuizChannel channel,
            QuizFeederConfiguration feederConfiguration,
            IFeederHandler<QuizChannel, QuizChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            var sessionStore = serviceProvider.GetRequiredService<QuizGameSessionStore>();
            var session = sessionStore.GetOrCreateSession(DemoGameId);

            _gameLoop = new QuizGameLoop(session, QuizQuestionBank.CreateDefault(), feederConfiguration);

            channel.SubscriptionAdded += (_, _) => Interlocked.Increment(ref _activeSubscriptions);
            channel.SubscriptionRemoved += (_, _) => Interlocked.Decrement(ref _activeSubscriptions);
        }

        /// <summary>
        /// One tick of the authoritative game loop: waits exactly <see cref="QuizGameLoop.NextDelay"/>
        /// via a cancellable <see cref="Task.Delay(TimeSpan,CancellationToken)"/> (#189's own AC on
        /// cancellation stopping the loop promptly — identical to every other feeder in this
        /// codebase), then — only while at least one subscriber is present, exactly like
        /// <c>NowClockFeeder</c>/<c>StockListBasicDemoChannelFeeder</c> — advances by one step and
        /// yields the resulting state. Yields nothing when unsubscribed (the game is paused, not
        /// abandoned: it resumes from wherever it left off once a subscriber appears) or once
        /// <see cref="QuizGameLoop.Advance"/> itself returns null (GameOver already reached).
        /// </summary>
        protected override async IAsyncEnumerable<FeederReceivedMessage<QuizChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(_gameLoop.NextDelay, cancellationToken);

            if (Volatile.Read(ref _activeSubscriptions) <= 0)
                yield break;

            var message = _gameLoop.Advance();
            if (message is not null)
                yield return message;
        }
    }
}
