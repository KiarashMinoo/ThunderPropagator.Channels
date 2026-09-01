namespace ThunderPropagator.Channels.Games.RockPaperScissors.Models
{
    /// <summary>
    /// Thrown by <see cref="RockPaperScissorsContextInitializationHostedService{TContext}"/> when a
    /// context's <see cref="BaseRockPaperScissorsContext.InitializeAsync"/> fails during host startup —
    /// mirrors <c>ChatContextInitializationException</c> (#114). Never thrown for cancellation — an
    /// <see cref="OperationCanceledException"/> from a shutting-down host propagates unwrapped.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsContextInitializationException(Type providerType, Exception innerException)
        : Exception($"RockPaperScissors context initialization failed for provider '{providerType.Name}'.", innerException)
    {
        public Type ProviderType { get; } = providerType;
    }
}
