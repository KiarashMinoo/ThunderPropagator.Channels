using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    internal
#if !DEBUG
        sealed
#endif
        class QuizJoinSubscribeRequest : ISubscribeRequest
    {
        public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SubscribingKeys { get; init; }
        public required IReadOnlyCollection<string> SubscribingFields { get; init; }
        public required SubscriptionMode? SubscriptionMode { get; init; }
    }
}
