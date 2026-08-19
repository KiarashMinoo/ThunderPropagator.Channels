namespace ThunderPropagator.Channels.Chat.Models
{
    /// <summary>
    /// Thrown by <see cref="ChatContextInitializationHostedService{TChatContext}"/> when a chat
    /// context's <see cref="BaseChatContext.InitializeAsync"/> fails during host startup, wrapping the
    /// underlying failure with the concrete provider type so startup logs and exception handlers can
    /// tell which persistence provider failed to come up (#114). Never thrown for cancellation — an
    /// <see cref="OperationCanceledException"/> from a shutting-down host propagates unwrapped.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class ChatContextInitializationException(Type providerType, Exception innerException)
        : Exception($"Chat context initialization failed for provider '{providerType.Name}'.", innerException)
    {
        public Type ProviderType { get; } = providerType;
    }
}
