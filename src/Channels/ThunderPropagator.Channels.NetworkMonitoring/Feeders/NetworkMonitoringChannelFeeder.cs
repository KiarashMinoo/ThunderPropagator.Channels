using ThunderPropagator.Application.Feeders;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.NetworkMonitoring.Channel;
using ThunderPropagator.Channels.NetworkMonitoring.Feeders;
using ThunderPropagator.Channels.NetworkMonitoring.Messages;

namespace ThunderPropagator.Channels.NetworkMonitoring.Feeders
{
    internal
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannelFeeder : IterativeFeeder<NetworkMonitoringChannel, NetworkMonitoringChannelFeederMessage, NetworkMonitoringChannelFeederConfiguration>
    {
        private long _lastBytesReceived;
        private long _lastBytesSent;

        // Tracks active subscriptions locally via the channel's public SubscriptionAdded/Removed
        // events, since neither is exposed to feeder code any other way. Read with Volatile.Read
        // and written with Interlocked so the poll loop always sees the latest count.
        private int _activeSubscriptions;

        public NetworkMonitoringChannelFeeder(NetworkMonitoringChannel channel,
            NetworkMonitoringChannelFeederConfiguration feederConfiguration,
            IFeederHandler<NetworkMonitoringChannel, NetworkMonitoringChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            HealthName = nameof(NetworkMonitoringChannelFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];

            channel.SubscriptionAdded += (_, _) => Interlocked.Increment(ref _activeSubscriptions);
            channel.SubscriptionRemoved += (_, _) => Interlocked.Decrement(ref _activeSubscriptions);
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<NetworkMonitoringChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            if (Volatile.Read(ref _activeSubscriptions) <= 0)
                yield break;

            var networkInterfaces = NetworkInterface
                .GetAllNetworkInterfaces()
                .Select(networkInterface => new
                {
                    NetworkInterface = networkInterface,
                    IPStatistics = networkInterface.GetIPStatistics()
                })
                .ToArray();

            var bytesReceived = networkInterfaces.Sum(networkInterface => networkInterface.IPStatistics.BytesReceived);
            var bytesSent = networkInterfaces.Sum(networkInterface => networkInterface.IPStatistics.BytesSent);

            // Interlocked.Exchange (see #20) rather than a plain assignment: `long` can't be marked
            // volatile in C# (only pointer-sized-or-smaller types support it), so this is the correct
            // way to guard against a stale read/torn write of these fields, and conveniently returns
            // the previous value atomically in the same operation the delta below needs anyway.
            var previousBytesReceived = Interlocked.Exchange(ref _lastBytesReceived, bytesReceived);
            var previousBytesSent = Interlocked.Exchange(ref _lastBytesSent, bytesSent);

            var networkMonitoringChannelFeederMessage = new NetworkMonitoringChannelFeederMessage
            {
                DateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                BytesReceived = bytesReceived - previousBytesReceived,
                BytesSent = bytesSent - previousBytesSent
            };

            yield return networkMonitoringChannelFeederMessage;
        }
    }
}