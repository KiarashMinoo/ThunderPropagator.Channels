using ThunderPropagator.Application.Feeders;
using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Clock.Channel;
using ThunderPropagator.Channels.Clock.Feeders;
using ThunderPropagator.Channels.Clock.Messages;

namespace ThunderPropagator.Channels.Clock.Feeders
{
    internal
#if !DEBUG
        sealed
#endif
        class NowClockFeeder : IterativeFeeder<ClockChannel, ClockChannelFeederMessage, NowClockFeederConfiguration>
    {
        // Tracks active subscriptions locally via the channel's public SubscriptionAdded/Removed
        // events, since neither is exposed to feeder code any other way. Read with Volatile.Read
        // and written with Interlocked so the poll loop always sees the latest count.
        private int _activeSubscriptions;

        public NowClockFeeder(ClockChannel channel,
            NowClockFeederConfiguration feederConfiguration,
            IFeederHandler<ClockChannel, ClockChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            HealthName = nameof(NowClockFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];

            channel.SubscriptionAdded += (_, _) => Interlocked.Increment(ref _activeSubscriptions);
            channel.SubscriptionRemoved += (_, _) => Interlocked.Decrement(ref _activeSubscriptions);
        }


        protected override async IAsyncEnumerable<FeederReceivedMessage<ClockChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);

            if (Volatile.Read(ref _activeSubscriptions) <= 0)
                yield break;

            yield return new ClockChannelFeederMessage(nameof(DateTime.Now), DateTime.Now);
        }
    }
}