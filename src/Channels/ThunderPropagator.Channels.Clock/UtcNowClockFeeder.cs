using ThunderPropagator.Application.Feeders;
using System.Runtime.CompilerServices;

namespace ThunderPropagator.Channels.Clock
{
    internal
#if !DEBUG
        sealed
#endif
        class UtcNowClockFeeder : IterativeFeeder<ClockChannel, ClockChannelFeederMessage, UtcNowClockFeederConfiguration>
    {
        public UtcNowClockFeeder(ClockChannel channel,
            UtcNowClockFeederConfiguration feederConfiguration,
            IFeederHandler<ClockChannel, ClockChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            HealthName = nameof(UtcNowClockFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<ClockChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
            yield return new ClockChannelFeederMessage(nameof(DateTime.UtcNow), DateTime.UtcNow);
        }
    }
}