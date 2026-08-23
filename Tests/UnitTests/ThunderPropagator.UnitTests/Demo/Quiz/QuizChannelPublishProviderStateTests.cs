using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #194: covers QuizChannel.PublishProviderState — a host publishing its own
    /// externally-produced quiz state, entirely independent of QuizGameSessionStore/QuizGameLoop.
    /// Phase-specific requiredness (the one rule QuizChannelFeederMessage's own setters do not already
    /// enforce), and that a valid request maps losslessly into the emitted channel message (#194's own
    /// AC).
    /// </summary>
    public sealed class QuizChannelPublishProviderStateTests
    {
        private static QuizChannel CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(new QuizChannelConfiguration());
            serviceProvider.GetService(typeof(QuizGameSessionStore)).Returns(new QuizGameSessionStore());
            serviceProvider.GetService(typeof(QuizGameLoopRegistry)).Returns(new QuizGameLoopRegistry());

            var channel = new QuizChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        // Mirrors NotificationsChannelSubscriptionKeysTests.EnableSnapshotting — snapshotting is off by
        // default, so EmitMessage's own stored snapshot (what SearchSnapshotsAsync reads back) is only
        // observable once this is called.
        private static void EnableSnapshotting(QuizChannel channel)
        {
            for (var type = channel.Metadata.GetType(); type is not null; type = type.BaseType)
            {
                var method = type.GetMethod("SetChannelSnapshot", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method is null)
                    continue;

                var arguments = method.GetParameters()
                    .Select(parameter => parameter.Name == "isEnabled" ? true : Type.Missing)
                    .ToArray();

                method.Invoke(channel.Metadata,
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.OptionalParamBinding,
                    null, arguments, null);
                return;
            }

            throw new MissingMethodException(channel.Metadata.GetType().FullName, "SetChannelSnapshot");
        }

        private static QuizProviderPublishRequest CreateValidQuestionRequest() => new()
        {
            GameId = "game-1",
            Phase = QuizPhase.Question,
            QuestionText = "What is 2 + 2?",
            Options = ["3", "4", "5"],
            TimeRemaining = 12,
            QuestionIndex = 2,
            TotalQuestions = 20,
            Scoreboard = [new QuizScoreboardEntry("Alice", 1500), new QuizScoreboardEntry("Bob", 900)],
            CorrectAnswer = "4",
            Winner = string.Empty
        };

        [Fact]
        public void PublishProviderState_WithNullRequest_Throws()
        {
            var channel = CreateChannel();

            Assert.Throws<ArgumentNullException>(() => channel.PublishProviderState(null!));
        }

        [Fact]
        public void PublishProviderState_ValidQuestionRequest_DoesNotThrow()
        {
            var channel = CreateChannel();

            var exception = Record.Exception(() => channel.PublishProviderState(CreateValidQuestionRequest()));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(QuizPhase.Question)]
        [InlineData(QuizPhase.Revealing)]
        public void PublishProviderState_WithEmptyQuestionTextDuringQuestionOrRevealing_Throws(QuizPhase phase)
        {
            var channel = CreateChannel();
            var request = CreateValidQuestionRequest() with { Phase = phase, QuestionText = "" };

            var exception = Assert.Throws<QuizProviderValidationException>(() => channel.PublishProviderState(request));

            Assert.Equal(nameof(QuizProviderPublishRequest.QuestionText), exception.PropertyName);
        }

        [Theory]
        [InlineData(QuizPhase.Question)]
        [InlineData(QuizPhase.Revealing)]
        public void PublishProviderState_WithFewerThanTwoOptionsDuringQuestionOrRevealing_Throws(QuizPhase phase)
        {
            var channel = CreateChannel();
            var request = CreateValidQuestionRequest() with { Phase = phase, Options = ["only one"] };

            var exception = Assert.Throws<QuizProviderValidationException>(() => channel.PublishProviderState(request));

            Assert.Equal(nameof(QuizProviderPublishRequest.Options), exception.PropertyName);
        }

        [Theory]
        [InlineData(QuizPhase.Lobby)]
        [InlineData(QuizPhase.Scoreboard)]
        [InlineData(QuizPhase.GameOver)]
        public void PublishProviderState_WithEmptyQuestionTextOutsideQuestionOrRevealing_Succeeds(QuizPhase phase)
        {
            var channel = CreateChannel();
            var request = new QuizProviderPublishRequest { GameId = "game-1", Phase = phase };

            var exception = Record.Exception(() => channel.PublishProviderState(request));

            Assert.Null(exception);
        }

        [Fact]
        public void PublishProviderState_WithNullOrWhiteSpaceGameId_ThrowsTheWireMessagesOwnValidationException()
        {
            // GameId's own requiredness is already enforced by QuizChannelFeederMessage's setter
            // (#186) — this proves that validation still fires along this new call path too, rather
            // than needing to be duplicated here.
            var channel = CreateChannel();
            var request = new QuizProviderPublishRequest { GameId = " ", Phase = QuizPhase.Lobby };

            Assert.Throws<QuizChannelFeederMessageValidationException>(() => channel.PublishProviderState(request));
        }

        [Fact]
        public async Task PublishProviderState_ValidRequest_MapsLosslesslyIntoTheEmittedSnapshot()
        {
            var channel = CreateChannel();
            EnableSnapshotting(channel);
            var request = CreateValidQuestionRequest();

            channel.PublishProviderState(request);

            var snapshotEntries = await channel.SearchSnapshotsAsync(entries => entries, 0, 0, CancellationToken.None);
            var entry = Assert.Single(snapshotEntries);

            Assert.Equal(request.GameId, entry.Snapshot[nameof(QuizChannelFeederMessage.GameId)]);
            Assert.Equal(request.Phase, entry.Snapshot[nameof(QuizChannelFeederMessage.Phase)]);
            Assert.Equal(request.QuestionText, entry.Snapshot[nameof(QuizChannelFeederMessage.QuestionText)]);
            Assert.Equal(request.Options, entry.Snapshot[nameof(QuizChannelFeederMessage.Options)]);
            Assert.Equal(request.TimeRemaining, entry.Snapshot[nameof(QuizChannelFeederMessage.TimeRemaining)]);
            Assert.Equal(request.QuestionIndex, entry.Snapshot[nameof(QuizChannelFeederMessage.QuestionIndex)]);
            Assert.Equal(request.TotalQuestions, entry.Snapshot[nameof(QuizChannelFeederMessage.TotalQuestions)]);
            Assert.Equal(request.Scoreboard, entry.Snapshot[nameof(QuizChannelFeederMessage.Scoreboard)]);
            Assert.Equal(request.CorrectAnswer, entry.Snapshot[nameof(QuizChannelFeederMessage.CorrectAnswer)]);
        }

        [Fact]
        public async Task PublishProviderState_DoesNotDependOnAnyExistingSession()
        {
            // The whole point of a provider-driven publish: no QuizGameSessionStore session needs to
            // exist for this GameId at all (#194's own "without coupling to the built-in simulation").
            var channel = CreateChannel();
            EnableSnapshotting(channel);

            var exception = Record.Exception(() => channel.PublishProviderState(CreateValidQuestionRequest()));

            Assert.Null(exception);
            var snapshotEntries = await channel.SearchSnapshotsAsync(entries => entries, 0, 0, CancellationToken.None);
            Assert.Single(snapshotEntries);
        }
    }
}
