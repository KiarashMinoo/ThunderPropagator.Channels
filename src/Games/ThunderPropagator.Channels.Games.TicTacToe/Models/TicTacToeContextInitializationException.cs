namespace ThunderPropagator.Channels.Games.TicTacToe.Models
{
    /// <summary>
    /// Thrown by <see cref="TicTacToeContextInitializationHostedService{TContext}"/> when a context's
    /// <see cref="BaseTicTacToeContext.InitializeAsync"/> fails during host startup — mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors's own RockPaperScissorsContextInitializationException.
    /// Never thrown for cancellation — an <see cref="OperationCanceledException"/> from a shutting-down
    /// host propagates unwrapped.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class TicTacToeContextInitializationException(Type providerType, Exception innerException)
        : Exception($"TicTacToe context initialization failed for provider '{providerType.Name}'.", innerException)
    {
        public Type ProviderType { get; } = providerType;
    }
}
