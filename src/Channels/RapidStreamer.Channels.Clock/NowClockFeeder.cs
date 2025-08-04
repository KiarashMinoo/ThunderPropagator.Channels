using RapidStreamer.Application.Feeders;
using System.Runtime.CompilerServices;

namespace RapidStreamer.Channels.Clock
{
    internal
#if !DEBUG
        sealed
#endif
        class NowClockFeeder : IterativeFeeder<ClockChannel, ClockChannelFeederMessage, NowClockFeederConfiguration>
    {
        public NowClockFeeder(ClockChannel channel,
            NowClockFeederConfiguration feederConfiguration,
            IFeederHandler<ClockChannel, ClockChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            HealthName = nameof(NowClockFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];
        }


        protected override async IAsyncEnumerable<FeederReceivedMessage<ClockChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
            yield return new ClockChannelFeederMessage(nameof(DateTime.Now), DateTime.Now);
        }
    }
}