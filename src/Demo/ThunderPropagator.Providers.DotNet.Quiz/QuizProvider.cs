using ThunderPropagator.Channels.Demo.Quiz;

namespace ThunderPropagator.Providers.DotNet.Quiz
{
    /// <summary>
    /// Publishes externally-produced quiz state into <see cref="QuizChannel"/> on demand — #194's own
    /// AC: "A host can publish valid quiz state programmatically." A thin wrapper around
    /// <see cref="QuizChannel.PublishProviderState"/>, which owns every actual validation rule
    /// (phase-specific requiredness, and everything <see cref="QuizChannelFeederMessage"/>'s own
    /// property setters already enforce); this type only ever adds the cancellation/enabled checks that
    /// are specific to being called through <see cref="IProvider{TChannel,TMessage}"/> at all.
    /// </summary>
    public sealed class QuizProvider(QuizChannel channel, QuizProviderConfiguration configuration) : IProvider<QuizChannel, QuizProviderMessage>
    {
        /// <summary>
        /// Checked, in order: <paramref name="cancellationToken"/> must not already be cancelled
        /// (#194's own AC: "Cancellation reaches provider execution" — checked before anything else, so
        /// a cancelled call never touches the channel at all); <see cref="QuizProviderConfiguration.IsEnabled"/>
        /// must be <see langword="true"/> (<see cref="QuizProviderDisabledException"/> otherwise); then
        /// <paramref name="message"/> is mapped losslessly onto <see cref="QuizProviderPublishRequest"/>
        /// and published via <see cref="QuizChannel.PublishProviderState"/>, whose own validation
        /// exceptions (<see cref="QuizProviderValidationException"/>,
        /// <see cref="QuizChannelFeederMessageValidationException"/>) propagate unchanged — never
        /// caught or wrapped here.
        /// </summary>
        public Task PublishAsync(QuizProviderMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();

            if (!configuration.IsEnabled)
                throw new QuizProviderDisabledException();

            channel.PublishProviderState(message.ToPublishRequest());

            return Task.CompletedTask;
        }
    }
}
