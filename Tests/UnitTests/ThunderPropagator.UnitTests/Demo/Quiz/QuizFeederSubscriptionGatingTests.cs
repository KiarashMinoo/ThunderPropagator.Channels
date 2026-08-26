using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.UnitTests.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Channel;
using ThunderPropagator.Channels.Demo.Quiz.Configuration;
using ThunderPropagator.Channels.Demo.Quiz.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Messages;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// QuizFeeder gates yielding on active subscriptions exactly like every other IterativeFeeder in
    /// this codebase (see e.g. NowClockFeederSubscriptionGatingTests) — the game is paused rather than
    /// abandoned while nobody is watching, and resumes counting down from wherever it left off once a
    /// subscriber (re)appears. Every configured duration is shrunk to a few milliseconds here purely to
    /// keep these tests fast; #189's own AC about the default 10s/15s/3s/5s durations is covered by
    /// QuizFeederConfigurationTests instead.
    /// </summary>
    public sealed class QuizFeederSubscriptionGatingTests
    {
        private static (QuizFeeder Feeder, QuizChannel Channel) CreateFeeder()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<QuizChannelFeederMessage, QuizFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new QuizChannelConfiguration());
            serviceProvider.RegisterService(new QuizGameSessionStore());
            serviceProvider.RegisterService(new QuizGameLoopRegistry());

            var channel = new QuizChannel(serviceProvider);
            var feederConfiguration = new QuizFeederConfiguration
            {
                LobbyDuration = TimeSpan.FromMilliseconds(1),
                QuestionDuration = TimeSpan.FromMilliseconds(1),
                RevealingDuration = TimeSpan.FromMilliseconds(1),
                ScoreboardDuration = TimeSpan.FromMilliseconds(1)
            };
            var feederHandler = new NoOpFeederHandler<QuizChannel, QuizChannelFeederMessage>();

            var feeder = new QuizFeeder(channel, feederConfiguration, feederHandler, serviceProvider);
            return (feeder, channel);
        }

        private static async Task<int> CountEmittedAsync(QuizFeeder feeder)
        {
            var count = 0;
            await foreach (var _ in FeederCancellationTestHelper.InvokeReceiveAsync<QuizChannelFeederMessage>(feeder, CancellationToken.None))
                count++;
            return count;
        }

        [Fact]
        public async Task ReceiveAsync_NoActiveSubscriptions_YieldsNoMessages()
        {
            var (feeder, _) = CreateFeeder();

            Assert.Equal(0, await CountEmittedAsync(feeder));
        }

        [Fact]
        public async Task ReceiveAsync_AfterSubscriptionAdded_YieldsMessage()
        {
            var (feeder, channel) = CreateFeeder();

            ChannelSubscriptionTestHelper.RaiseSubscriptionAdded(channel);

            Assert.Equal(1, await CountEmittedAsync(feeder));
        }

        [Fact]
        public async Task ReceiveAsync_AfterSubscribeThenUnsubscribe_YieldsNoMessages()
        {
            var (feeder, channel) = CreateFeeder();

            ChannelSubscriptionTestHelper.RaiseSubscriptionAdded(channel);
            ChannelSubscriptionTestHelper.RaiseSubscriptionRemoved(channel);

            Assert.Equal(0, await CountEmittedAsync(feeder));
        }

        [Fact]
        public async Task ReceiveAsync_WhileUnsubscribed_NeverAdvancesThePhase()
        {
            var (feeder, _) = CreateFeeder();

            for (var i = 0; i < 5; i++)
                await CountEmittedAsync(feeder);

            // Nothing was ever yielded, so nothing was ever recorded as the session's current state —
            // the clearest external evidence the loop truly paused rather than silently advancing
            // while unobserved.
            Assert.Null(GetSession(feeder).CurrentState);
        }

        private static QuizGameSession GetSession(QuizFeeder feeder)
        {
            var gameLoop = typeof(QuizFeeder).GetField("_gameLoop", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(feeder)!;
            return (QuizGameSession)gameLoop.GetType().GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(gameLoop)!;
        }
    }
}
