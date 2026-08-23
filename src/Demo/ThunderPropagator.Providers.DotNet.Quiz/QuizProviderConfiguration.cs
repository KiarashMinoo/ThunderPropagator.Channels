namespace ThunderPropagator.Providers.DotNet.Quiz
{
    /// <summary>Settings for <see cref="QuizProvider"/>.</summary>
    public sealed class QuizProviderConfiguration
    {
        /// <summary>Whether <see cref="QuizProvider.PublishAsync"/> accepts publications at all. Default: <see langword="true"/>. When <see langword="false"/>, every call throws <see cref="QuizProviderDisabledException"/> without touching the channel.</summary>
        public bool IsEnabled { get; set; } = true;
    }
}
