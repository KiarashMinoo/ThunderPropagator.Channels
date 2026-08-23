using ThunderPropagator.Application.Channels.Subscribers;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>The outcome of <see cref="QuizChannel.Join"/>, unpacked by <c>QuizJoinGameReceiverPipeline</c> into its own public response DTO.</summary>
    internal sealed record QuizJoinResult(Subscription Subscription, bool IsReconnect, bool IsHost, string PlayerName);
}
