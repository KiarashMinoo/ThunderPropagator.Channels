using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.UnitTests.Feeders;
using ThunderPropagator.Channels.Clock.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Channel;
using ThunderPropagator.Channels.Demo.Quiz.Configuration;
using ThunderPropagator.Channels.Demo.Quiz.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Messages;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #189's own AC: "Cancellation and host shutdown stop the loop promptly." QuizFeeder's
    /// default LobbyDuration (10s) is long enough to cancel well before it elapses, exactly like
    /// NowClockFeederCancellationTests does against NowClockFeeder's own fixed delay.
    /// </summary>
    public sealed class QuizFeederCancellationTests
    {
        [Fact]
        public async Task ReceiveAsync_CancelledDuringDelay_StopsPromptly()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<QuizChannelFeederMessage, QuizFeederConfiguration>();
            var channelConfiguration = new QuizChannelConfiguration();
            serviceProvider.RegisterChannelConfiguration(channelConfiguration);
            serviceProvider.RegisterService(new QuizGameSessionStore());
            serviceProvider.RegisterService(new QuizGameLoopRegistry());

            var channel = new QuizChannel(serviceProvider);
            var feederConfiguration = new QuizFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<QuizChannel, QuizChannelFeederMessage>();

            var feeder = new QuizFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            using var cancellationTokenSource = new CancellationTokenSource();

            // QuizFeeder waits the full 10s default LobbyDuration before its first tick; cancel well
            // before that elapses and confirm the enumeration is cancelled promptly instead of waiting
            // out the delay.
            await FeederCancellationTestHelper.AssertCancelledDuringDelayAsync<QuizChannelFeederMessage>(
                feeder,
                cancellationTokenSource,
                cancelAfter: TimeSpan.FromMilliseconds(50),
                promptTimeout: TimeSpan.FromSeconds(2));
        }
    }
}
