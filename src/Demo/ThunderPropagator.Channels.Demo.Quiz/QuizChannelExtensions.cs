using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Answer;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Join;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Start;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    public static class QuizChannelExtensions
    {
        // Issue #183 registered only the channel itself; #189 added the game-loop IterativeFeeder and,
        // with it, the first real consumer of QuizGameSessionStore (#187), making that store a
        // singleton. #192 adds QuizGameLoopRegistry as a singleton too, since that's how QuizChannel's
        // SubmitAnswer/StartGame reach the very same QuizGameLoop instance the feeder constructs and
        // registers into it. #191/#192/#193 add the Join/Answer/Start receive pipelines on top.
        public static IServiceCollection AddQuizChannel(this IServiceCollection services, Action<QuizChannelConfiguration>? channelConfigurator = null)
        {
            QuizChannelConfiguration quizChannelConfiguration = new();
            channelConfigurator?.Invoke(quizChannelConfiguration);

            services
                .AddSingleton(quizChannelConfiguration)
                .AddSingleton<QuizGameSessionStore>()
                .AddSingleton<QuizGameLoopRegistry>()
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
