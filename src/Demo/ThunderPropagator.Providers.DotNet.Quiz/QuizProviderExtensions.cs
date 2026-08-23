using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Demo.Quiz;

namespace ThunderPropagator.Providers.DotNet.Quiz
{
    public static class QuizProviderExtensions
    {
        /// <summary>
        /// Registers <see cref="QuizProvider"/> as the <see cref="IProvider{TChannel,TMessage}"/> for
        /// <see cref="QuizChannel"/> — #194's own scope: "Register through AddChannelProvider". Purely
        /// additive: it assumes <c>QuizChannelExtensions.AddQuizChannel</c> has already registered
        /// <see cref="QuizChannel"/> in the same container (this resolves it, it does not register it
        /// itself), so a host adds this on top rather than in place of that call — the built-in
        /// simulation (#189) keeps running its own fixed demo GameId regardless; see
        /// <see cref="QuizChannel.PublishProviderState"/>'s own remarks on the two coexisting safely
        /// only for different GameIds.
        /// </summary>
        public static IServiceCollection AddChannelProvider(this IServiceCollection services, Action<QuizProviderConfiguration>? providerConfigurator = null)
        {
            QuizProviderConfiguration providerConfiguration = new();
            providerConfigurator?.Invoke(providerConfiguration);

            services
                .AddSingleton(providerConfiguration)
                .AddSingleton<IProvider<QuizChannel, QuizProviderMessage>, QuizProvider>();

            return services;
        }
    }
}
