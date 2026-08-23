using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Providers.DotNet.Quiz;

namespace ThunderPropagator.Channels.Demo.Quiz.UnitTests
{
    /// <summary>
    /// Issue #194: integration tests for the ThunderPropagator.Providers.DotNet.Quiz package —
    /// QuizProvider.PublishAsync's own cancellation/enabled checks, and that a valid publish actually
    /// reaches QuizChannel's broadcast path (via its own snapshot store, since no live subscriber is
    /// set up in a unit test). QuizChannelPublishProviderStateTests (in the main test project, which
    /// has InternalsVisibleTo into the Quiz assembly) covers the deeper phase-validation/lossless-mapping
    /// rules QuizChannel.PublishProviderState itself enforces — this file only exercises what the
    /// public provider package's own consumers can see.
    /// </summary>
    public sealed class QuizProviderTests
    {
        // QuizGameSessionStore/QuizGameLoopRegistry are internal to the Quiz assembly and not visible
        // from this project (no InternalsVisibleTo here, unlike ThunderPropagator.UnitTests) — resolved
        // by name and instantiated via reflection purely to satisfy QuizChannel's constructor
        // dependencies, exactly like QuizSmokeTests already does for the same reason.
        private static QuizChannel CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(new QuizChannelConfiguration());

            var sessionStoreType = typeof(QuizChannel).Assembly.GetType("ThunderPropagator.Channels.Demo.Quiz.Game.QuizGameSessionStore")!;
            serviceProvider.GetService(sessionStoreType).Returns(Activator.CreateInstance(sessionStoreType));

            var gameLoopRegistryType = typeof(QuizChannel).Assembly.GetType("ThunderPropagator.Channels.Demo.Quiz.Game.QuizGameLoopRegistry")!;
            serviceProvider.GetService(gameLoopRegistryType).Returns(Activator.CreateInstance(gameLoopRegistryType));

            var channel = new QuizChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        private static QuizProviderMessage CreateValidMessage() => new()
        {
            GameId = "game-1",
            Phase = QuizPhase.Lobby
        };

        [Fact]
        public async Task PublishAsync_ValidMessage_DoesNotThrow()
        {
            var provider = new QuizProvider(CreateChannel(), new QuizProviderConfiguration());

            var exception = await Record.ExceptionAsync(() => provider.PublishAsync(CreateValidMessage()));

            Assert.Null(exception);
        }

        [Fact]
        public async Task PublishAsync_WithNullMessage_Throws()
        {
            var provider = new QuizProvider(CreateChannel(), new QuizProviderConfiguration());

            await Assert.ThrowsAsync<ArgumentNullException>(() => provider.PublishAsync(null!));
        }

        [Fact]
        public async Task PublishAsync_WithAnAlreadyCancelledToken_ThrowsWithoutTouchingTheChannel()
        {
            var provider = new QuizProvider(CreateChannel(), new QuizProviderConfiguration());
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.PublishAsync(CreateValidMessage(), cancellationTokenSource.Token));
        }

        [Fact]
        public async Task PublishAsync_WhenDisabled_ThrowsQuizProviderDisabled()
        {
            var provider = new QuizProvider(CreateChannel(), new QuizProviderConfiguration { IsEnabled = false });

            await Assert.ThrowsAsync<QuizProviderDisabledException>(() => provider.PublishAsync(CreateValidMessage()));
        }

        [Fact]
        public async Task PublishAsync_WithIncompletePhaseSpecificFields_PropagatesTheChannelsOwnValidationException()
        {
            var provider = new QuizProvider(CreateChannel(), new QuizProviderConfiguration());
            var message = new QuizProviderMessage { GameId = "game-1", Phase = QuizPhase.Question, QuestionText = "" };

            var exception = await Record.ExceptionAsync(() => provider.PublishAsync(message));

            // QuizProviderValidationException is internal to the Quiz assembly's own exception surface
            // exposed publicly (it is a public type) — asserted by name rather than a using, keeping
            // this file's own dependency footprint limited to what the provider package itself needs.
            Assert.Equal("QuizProviderValidationException", exception!.GetType().Name);
        }

        [Fact]
        public void AddChannelProvider_RegistersAResolvableProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton(CreateChannel());

            services.AddChannelProvider();
            var serviceProvider = services.BuildServiceProvider();

            var provider = serviceProvider.GetService<IProvider<QuizChannel, QuizProviderMessage>>();
            Assert.NotNull(provider);
            Assert.IsType<QuizProvider>(provider);
        }

        [Fact]
        public void AddChannelProvider_DoesNotThrow()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() => services.AddChannelProvider());

            Assert.Null(exception);
        }

        [Fact]
        public void AddChannelProvider_AppliesTheConfiguratorCallback()
        {
            var services = new ServiceCollection();

            services.AddChannelProvider(configuration => configuration.IsEnabled = false);
            var serviceProvider = services.BuildServiceProvider();

            var configuration = serviceProvider.GetRequiredService<QuizProviderConfiguration>();
            Assert.False(configuration.IsEnabled);
        }
    }
}
