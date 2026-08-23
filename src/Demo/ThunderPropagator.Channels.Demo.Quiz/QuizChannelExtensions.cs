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
    }
}
