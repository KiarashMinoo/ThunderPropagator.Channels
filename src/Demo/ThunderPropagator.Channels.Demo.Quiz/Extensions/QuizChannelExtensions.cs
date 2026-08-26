using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Answer;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Join;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Start;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Demo.Quiz.Channel;
using ThunderPropagator.Channels.Demo.Quiz.Configuration;
using ThunderPropagator.Channels.Demo.Quiz.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Messages;

namespace ThunderPropagator.Channels.Demo.Quiz.Extensions
{
    public static class QuizChannelExtensions
    {
        // Issue #183 registered only the channel itself; #189 added the game-loop IterativeFeeder and,
        // with it, the first real consumer of QuizGameSessionStore (#187), making that store a
        // singleton. #192 adds QuizGameLoopRegistry as a singleton too, since that's how QuizChannel's
        // SubmitAnswer/StartGame reach the very same QuizGameLoop instance the feeder constructs and
        // registers into it. #191/#192/#193 add the Join/Answer/Start receive pipelines on top. #195's
        // own AC ("Make repeated registration safe") is why every singleton below is a TryAdd* rather
        // than a plain Add*: AddChannel/AddChannelFeeder/AddReceivePipeline are already TryAdd*
        // internally, so with these three also converted, a second AddQuizChannel call on the same
        // IServiceCollection is a complete no-op — the first call's configuration wins, matching
        // NotificationsExtensions.AddNotificationsChannel's own established convention for the same
        // AC elsewhere in this codebase.
        public static IServiceCollection AddQuizChannel(this IServiceCollection services, Action<QuizChannelConfiguration>? channelConfigurator = null)
        {
            QuizChannelConfiguration quizChannelConfiguration = new();
            channelConfigurator?.Invoke(quizChannelConfiguration);

            // Each property validates itself against rules only it can check (positivity) the moment
            // channelConfigurator sets it; this is the one rule that needs both properties at once, so
            // it can only run here, after the configurator above has finished (#195's own AC: "Invalid
            // configuration fails at startup with property-specific errors").
            if (quizChannelConfiguration.MinPlayers > quizChannelConfiguration.MaxPlayers)
                throw new QuizChannelConfigurationValidationException(nameof(QuizChannelConfiguration.MinPlayers), $"({quizChannelConfiguration.MinPlayers}) must not exceed {nameof(QuizChannelConfiguration.MaxPlayers)} ({quizChannelConfiguration.MaxPlayers}).");

            services.TryAddSingleton(quizChannelConfiguration);
            services.TryAddSingleton<QuizGameSessionStore>();
            services.TryAddSingleton<QuizGameLoopRegistry>();

            services
                .AddChannel<QuizChannel>()
                .AddChannelFeeder<QuizChannel, QuizFeeder, QuizChannelFeederMessage, QuizFeederConfiguration>(configuration =>
                    configuration.Bind(quizChannelConfiguration.FeederConfiguration))
                .AddReceivePipeline<QuizChannel, QuizJoinGameReceiverPipeline>()
                .AddReceivePipeline<QuizChannel, QuizSubmitAnswerReceiverPipeline>()
                .AddReceivePipeline<QuizChannel, QuizStartGameReceiverPipeline>();

            return services;
        }

        /// <summary>
        /// Registers <see cref="QuizChannel"/>'s own <see cref="IProvider{TMessage}"/> implementation
        /// (#194's own scope: "Register through AddChannelProvider") so it can be resolved as
        /// <see cref="IProvider{TMessage}"/> of <see cref="QuizProviderMessage"/> without a caller
        /// needing to know the concrete channel type. Purely additive: it assumes
        /// <see cref="AddQuizChannel"/> has already registered <see cref="QuizChannel"/> in the same
        /// container (this resolves that same singleton, it does not register a separate instance), so
        /// a host adds this on top rather than in place of that call — the built-in simulation (#189)
        /// keeps running its own fixed demo GameId regardless; see
        /// <see cref="QuizChannel.PublishAsync"/>'s own remarks on the two coexisting safely only for
        /// different GameIds.
        /// </summary>
        public static IServiceCollection AddChannelProvider(this IServiceCollection services)
        {
            services.AddSingleton<IProvider<QuizProviderMessage>>(serviceProvider => serviceProvider.GetRequiredService<QuizChannel>());

            return services;
        }
    }
}
