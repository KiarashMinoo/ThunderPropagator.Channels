using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Channels.Games.RockPaperScissors.Configuration;
using ThunderPropagator.Channels.Games.RockPaperScissors.Metadata;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.Channel
{
    public
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsChannel : AbstractChannel<RockPaperScissorsChannelMetadata, RockPaperScissorsChannelConfiguration>
    {
        public RockPaperScissorsChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        internal Subscription? PeekRandomPlayer()
        {
            if (Subscriptions.SubscriptionCount <= 0)
            {
                return null;
            }

            var randomizedIndex = Random.Shared.Next(Subscriptions.SubscriptionCount);
            return Subscriptions.Subscriptions[randomizedIndex];
        }

        internal Task SendAsync(Subscription subscription, IReadOnlyDictionary<string, object?> feederMessage, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
            //return base.SendAsync(subscription, feederMessage, false, 'N', cancellationToken);
        }
    }
}