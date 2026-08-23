using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Join;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    public static class QuizChannelExtensions
    {
        // Issue #183 registered only the channel itself; #189 added the game-loop IterativeFeeder and,
        // with it, the first real consumer of QuizGameSessionStore (#187), making that store a
        // singleton. #191 adds the first receive pipeline — QuizChannel.Join (which every future
        // pipeline like #192/#193 will extend alongside) resolves that very same session store.
        public static IServiceCollection AddQuizChannel(this IServiceCollection services, Action<QuizChannelConfiguration>? channelConfigurator = null)
        {
            QuizChannelConfiguration quizChannelConfiguration = new();
            channelConfigurator?.Invoke(quizChannelConfiguration);

            services
                .AddSingleton(quizChannelConfiguration)
                .AddSingleton<QuizGameSessionStore>()
                .AddChannel<QuizChannel>()
                .AddChannelFeeder<QuizChannel, QuizFeeder, QuizChannelFeederMessage, QuizFeederConfiguration>(configuration =>
                    configuration.Bind(quizChannelConfiguration.FeederConfiguration))
                .AddReceivePipeline<QuizChannel, QuizJoinGameReceiverPipeline>();

            return services;
        }
    }
}
