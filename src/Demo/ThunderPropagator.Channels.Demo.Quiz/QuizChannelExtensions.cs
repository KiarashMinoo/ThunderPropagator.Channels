using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    public static class QuizChannelExtensions
    {
        // Issue #183 registered only the channel itself; #189 adds the game-loop IterativeFeeder and,
        // with it, the first real consumer of QuizGameSessionStore (#187), so this is also where that
        // store first becomes a singleton — every future receive pipeline (#191/#192/#193 add
        // Join/Answer/Start) resolves the very same instance QuizFeeder already drives its demo game
        // through.
        public static IServiceCollection AddQuizChannel(this IServiceCollection services, Action<QuizChannelConfiguration>? channelConfigurator = null)
        {
            QuizChannelConfiguration quizChannelConfiguration = new();
            channelConfigurator?.Invoke(quizChannelConfiguration);

            services
                .AddSingleton(quizChannelConfiguration)
                .AddSingleton<QuizGameSessionStore>()
                .AddChannel<QuizChannel>()
                .AddChannelFeeder<QuizChannel, QuizFeeder, QuizChannelFeederMessage, QuizFeederConfiguration>(configuration =>
                    configuration.Bind(quizChannelConfiguration.FeederConfiguration));

            return services;
        }
    }
}
