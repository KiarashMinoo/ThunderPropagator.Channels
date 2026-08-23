namespace ThunderPropagator.Providers.DotNet.Quiz
{
    /// <summary>Thrown by <see cref="QuizProvider.PublishAsync"/> when <see cref="QuizProviderConfiguration.IsEnabled"/> is <see langword="false"/>.</summary>
    public sealed class QuizProviderDisabledException() : Exception("The quiz provider is disabled.");
}
