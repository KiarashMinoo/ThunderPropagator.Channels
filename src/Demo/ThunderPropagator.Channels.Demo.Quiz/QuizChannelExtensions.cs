using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    public static class QuizChannelExtensions
    {
        // Issue #183: registers only the channel itself for now — no receive pipelines exist yet
        // (#191/#192/#193 add Join/Answer/Start) and no feeder exists yet (#189 adds the game-loop
        // IterativeFeeder).
        public static IServiceCollection AddQuizChannel(this IServiceCollection services, Action<QuizChannelConfiguration>? channelConfigurator = null)
        {
            QuizChannelConfiguration quizChannelConfiguration = new();
            channelConfigurator?.Invoke(quizChannelConfiguration);

            services
                .AddSingleton(quizChannelConfiguration)
                .AddChannel<QuizChannel>();

            return services;
        }
    }
}
