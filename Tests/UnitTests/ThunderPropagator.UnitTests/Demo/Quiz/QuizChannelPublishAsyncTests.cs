using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels.Exceptions;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #194: covers QuizChannel's own IProvider{QuizProviderMessage} implementation
    /// (PublishAsync) — a host publishing its own externally-produced quiz state, entirely independent
    /// of QuizGameSessionStore/QuizGameLoop. Cancellation/enabled checks, phase-specific requiredness
    /// (the one rule QuizChannelFeederMessage's own setters do not already enforce), that a valid
    /// message maps losslessly into the emitted channel message, and AddChannelProvider's registration
    /// (#194's own AC).
    /// </summary>
    public sealed class QuizChannelPublishAsyncTests
    {
        private static QuizChannel CreateChannel(QuizChannelConfiguration? channelConfiguration = null)
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(channelConfiguration ?? new QuizChannelConfiguration());
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

        private static QuizProviderMessage CreateValidQuestionMessage() => new()
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
        public async Task PublishAsync_WithNullMessage_Throws()
        {
            var channel = CreateChannel();

            await Assert.ThrowsAsync<ArgumentNullException>(() => channel.PublishAsync(null!));
        }

        [Fact]
        public async Task PublishAsync_ValidQuestionMessage_DoesNotThrow()
        {
            var channel = CreateChannel();

            var exception = await Record.ExceptionAsync(() => channel.PublishAsync(CreateValidQuestionMessage()));

            Assert.Null(exception);
        }

        [Fact]
        public async Task PublishAsync_WithAnAlreadyCancelledToken_ThrowsWithoutTouchingTheChannel()
        {
            var channel = CreateChannel();
            EnableSnapshotting(channel);
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => channel.PublishAsync(CreateValidQuestionMessage(), cancellationTokenSource.Token));

            var snapshotEntries = await channel.SearchSnapshotsAsync(entries => entries, 0, 0, CancellationToken.None);
            Assert.Empty(snapshotEntries);
        }

        [Fact]
        public async Task PublishAsync_WhenChannelIsDisabled_ThrowsChannelIsNotEnabled()
        {
            var channel = CreateChannel(new QuizChannelConfiguration { IsEnabled = false });

            await Assert.ThrowsAsync<ChannelIsNotEnabledException>(() => channel.PublishAsync(CreateValidQuestionMessage()));
        }

        [Theory]
        [InlineData(QuizPhase.Question)]
        [InlineData(QuizPhase.Revealing)]
        public async Task PublishAsync_WithEmptyQuestionTextDuringQuestionOrRevealing_Throws(QuizPhase phase)
        {
            var channel = CreateChannel();
            var message = CreateValidQuestionMessage() with { Phase = phase, QuestionText = "" };

            var exception = await Assert.ThrowsAsync<QuizProviderValidationException>(() => channel.PublishAsync(message));

            Assert.Equal(nameof(QuizProviderMessage.QuestionText), exception.PropertyName);
        }

        [Theory]
        [InlineData(QuizPhase.Question)]
        [InlineData(QuizPhase.Revealing)]
        public async Task PublishAsync_WithFewerThanTwoOptionsDuringQuestionOrRevealing_Throws(QuizPhase phase)
        {
            var channel = CreateChannel();
            var message = CreateValidQuestionMessage() with { Phase = phase, Options = ["only one"] };

            var exception = await Assert.ThrowsAsync<QuizProviderValidationException>(() => channel.PublishAsync(message));

            Assert.Equal(nameof(QuizProviderMessage.Options), exception.PropertyName);
        }

        [Theory]
        [InlineData(QuizPhase.Lobby)]
        [InlineData(QuizPhase.Scoreboard)]
        [InlineData(QuizPhase.GameOver)]
        public async Task PublishAsync_WithEmptyQuestionTextOutsideQuestionOrRevealing_Succeeds(QuizPhase phase)
        {
            var channel = CreateChannel();
            var message = new QuizProviderMessage { GameId = "game-1", Phase = phase };

            var exception = await Record.ExceptionAsync(() => channel.PublishAsync(message));

            Assert.Null(exception);
        }

        [Fact]
        public async Task PublishAsync_WithNullOrWhiteSpaceGameId_ThrowsTheWireMessagesOwnValidationException()
        {
            // GameId's own requiredness is already enforced by QuizChannelFeederMessage's setter
            // (#186) — this proves that validation still fires along this new call path too, rather
            // than needing to be duplicated here.
            var channel = CreateChannel();
            var message = new QuizProviderMessage { GameId = " ", Phase = QuizPhase.Lobby };

            await Assert.ThrowsAsync<QuizChannelFeederMessageValidationException>(() => channel.PublishAsync(message));
        }

        [Fact]
        public async Task PublishAsync_ValidMessage_MapsLosslesslyIntoTheEmittedSnapshot()
        {
            var channel = CreateChannel();
            EnableSnapshotting(channel);
            var message = CreateValidQuestionMessage();

            await channel.PublishAsync(message);

            var snapshotEntries = await channel.SearchSnapshotsAsync(entries => entries, 0, 0, CancellationToken.None);
            var entry = Assert.Single(snapshotEntries);

            Assert.Equal(message.GameId, entry.Snapshot[nameof(QuizChannelFeederMessage.GameId)]);
            Assert.Equal(message.Phase, entry.Snapshot[nameof(QuizChannelFeederMessage.Phase)]);
            Assert.Equal(message.QuestionText, entry.Snapshot[nameof(QuizChannelFeederMessage.QuestionText)]);
            Assert.Equal(message.Options, entry.Snapshot[nameof(QuizChannelFeederMessage.Options)]);
            Assert.Equal(message.TimeRemaining, entry.Snapshot[nameof(QuizChannelFeederMessage.TimeRemaining)]);
            Assert.Equal(message.QuestionIndex, entry.Snapshot[nameof(QuizChannelFeederMessage.QuestionIndex)]);
            Assert.Equal(message.TotalQuestions, entry.Snapshot[nameof(QuizChannelFeederMessage.TotalQuestions)]);
            Assert.Equal(message.Scoreboard, entry.Snapshot[nameof(QuizChannelFeederMessage.Scoreboard)]);
            Assert.Equal(message.CorrectAnswer, entry.Snapshot[nameof(QuizChannelFeederMessage.CorrectAnswer)]);
        }

        [Fact]
        public async Task PublishAsync_DoesNotDependOnAnyExistingSession()
        {
            // The whole point of a provider-driven publish: no QuizGameSessionStore session needs to
            // exist for this GameId at all (#194's own "without coupling to the built-in simulation").
            var channel = CreateChannel();
            EnableSnapshotting(channel);

            var exception = await Record.ExceptionAsync(() => channel.PublishAsync(CreateValidQuestionMessage()));

            Assert.Null(exception);
            var snapshotEntries = await channel.SearchSnapshotsAsync(entries => entries, 0, 0, CancellationToken.None);
            Assert.Single(snapshotEntries);
        }

        [Fact]
        public void AddChannelProvider_ResolvesQuizChannelAsIProvider()
        {
            var channel = CreateChannel();
            var services = new ServiceCollection();
            services.AddSingleton(channel);

            services.AddChannelProvider();
            var serviceProvider = services.BuildServiceProvider();

            var provider = serviceProvider.GetService<IProvider<QuizProviderMessage>>();
            Assert.Same(channel, provider);
        }

        [Fact]
        public void AddChannelProvider_DoesNotThrow()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() => services.AddChannelProvider());

            Assert.Null(exception);
        }
    }
}
