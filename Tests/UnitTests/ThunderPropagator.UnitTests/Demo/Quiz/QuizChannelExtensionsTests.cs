using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Channel;
using ThunderPropagator.Channels.Demo.Quiz.Configuration;
using ThunderPropagator.Channels.Demo.Quiz.Extensions;
using ThunderPropagator.Channels.Demo.Quiz.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Messages;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #195: covers <see cref="QuizChannelExtensions.AddQuizChannel"/>'s own DI-registration
    /// contract directly — a minimal host resolving the complete channel through one call, repeated
    /// invocation being safe, and invalid configuration failing with a property-specific error —
    /// distinct from the channel's own runtime behavior (already covered by QuizChannelJoinTests/
    /// QuizChannelSubmitAnswerTests/QuizChannelStartGameTests/QuizChannelPublishAsyncTests) and from
    /// <c>ThunderPropagator.Channels.Demo.Quiz.UnitTests.QuizSmokeTests.AddQuizChannel_DoesNotThrow</c>'s
    /// own shallower registration-only smoke test in the sibling cross-channel test project (that
    /// project has no <c>InternalsVisibleTo</c> into this assembly, so it cannot resolve
    /// <see cref="QuizGameSessionStore"/>/<see cref="QuizGameLoopRegistry"/> the way this one can).
    /// </summary>
    public sealed class QuizChannelExtensionsTests
    {
        // AddChannel<TChannel>'s own factory (see ChannelsExtensions) needs IHostApplicationLifetime
        // and ILoggerFactory resolvable to construct and initialize the channel — the same two
        // QuizSmokeTests.QuizChannel_ConstructsAndInitializes_WithoutThrowing supplies via NSubstitute
        // directly. Registering them for real here is what makes GetRequiredService<QuizChannel>()
        // below an actual end-to-end resolution through AddQuizChannel, not just a registration check.
        private static IServiceCollection CreateHostServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(Substitute.For<IHostApplicationLifetime>());
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            return services;
        }

        [Fact]
        public void AddQuizChannel_ResolvesTheCompleteChannel()
        {
            var services = CreateHostServices();
            services.AddQuizChannel();
            var serviceProvider = services.BuildServiceProvider();

            var exception = Record.Exception(() => serviceProvider.GetRequiredService<QuizChannel>());

            Assert.Null(exception);
        }

        [Fact]
        public void AddQuizChannel_RegistersAllThreeReceivePipelines()
        {
            var services = CreateHostServices();
            services.AddQuizChannel();
            var serviceProvider = services.BuildServiceProvider();

            var pipelines = serviceProvider.GetServices<IReceivePipeline<QuizChannel>>().ToList();

            Assert.Equal(3, pipelines.Count);
        }

        // Actually resolving IFeeder<QuizChannel> needs an IFeederHandler<QuizChannel,QuizChannelFeederMessage>
        // that comes from host-wide framework setup outside AddQuizChannel's own scope (a bare
        // ServiceCollection carrying only AddQuizChannel's own registrations cannot satisfy it) — so
        // this checks the registration AddQuizChannel itself owns exists, rather than resolving through
        // it, which would really be testing that missing host-wide wiring instead.
        [Fact]
        public void AddQuizChannel_RegistersTheFeederConfigurationAndTheFeederItself()
        {
            var services = CreateHostServices();

            services.AddQuizChannel();

            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(QuizFeederConfiguration));
            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFeeder<QuizChannel>));
        }

        [Fact]
        public void AddQuizChannel_CalledTwice_DoesNotThrow()
        {
            var services = CreateHostServices();

            var exception = Record.Exception(() =>
            {
                services.AddQuizChannel();
                services.AddQuizChannel();
            });

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(typeof(QuizChannelConfiguration))]
        [InlineData(typeof(QuizGameSessionStore))]
        [InlineData(typeof(QuizGameLoopRegistry))]
        public void AddQuizChannel_CalledTwice_RegistersEachSingletonExactlyOnce(Type serviceType)
        {
            var services = CreateHostServices();

            services.AddQuizChannel();
            services.AddQuizChannel();

            Assert.Single(services, descriptor => descriptor.ServiceType == serviceType);
        }

        [Fact]
        public void AddQuizChannel_CalledTwice_TheFirstCallsConfigurationWins()
        {
            var services = CreateHostServices();

            services.AddQuizChannel(configuration => configuration.MaxPlayers = 3);
            services.AddQuizChannel(configuration => configuration.MaxPlayers = 99);
            var serviceProvider = services.BuildServiceProvider();

            Assert.Equal(3, serviceProvider.GetRequiredService<QuizChannelConfiguration>().MaxPlayers);
        }

        [Fact]
        public void AddQuizChannel_ConfiguratorRuns_ResolvesTheConfiguredValues()
        {
            var services = CreateHostServices();

            services.AddQuizChannel(configuration =>
            {
                configuration.MaxPlayers = 16;
                configuration.MinPlayers = 3;
                configuration.AllowMidGameJoin = false;
            });
            var serviceProvider = services.BuildServiceProvider();
            var resolved = serviceProvider.GetRequiredService<QuizChannelConfiguration>();

            Assert.Equal(16, resolved.MaxPlayers);
            Assert.Equal(3, resolved.MinPlayers);
            Assert.False(resolved.AllowMidGameJoin);
        }

        [Fact]
        public void AddQuizChannel_WithMinPlayersGreaterThanMaxPlayers_ThrowsWithThePropertyNamed()
        {
            var services = CreateHostServices();

            var exception = Assert.Throws<QuizChannelConfigurationValidationException>(() =>
                services.AddQuizChannel(configuration =>
                {
                    configuration.MaxPlayers = 2;
                    configuration.MinPlayers = 5;
                }));

            Assert.Equal(nameof(QuizChannelConfiguration.MinPlayers), exception.PropertyName);
        }

        [Fact]
        public void AddQuizChannel_WithMinPlayersEqualToMaxPlayers_DoesNotThrow()
        {
            var services = CreateHostServices();

            var exception = Record.Exception(() =>
                services.AddQuizChannel(configuration =>
                {
                    configuration.MaxPlayers = 4;
                    configuration.MinPlayers = 4;
                }));

            Assert.Null(exception);
        }

        [Fact]
        public void AddQuizChannel_WithNonPositiveQuestionsPerGame_Throws()
        {
            var services = CreateHostServices();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                services.AddQuizChannel(configuration => configuration.FeederConfiguration.QuestionsPerGame = 0));
        }
    }
}
