using ThunderPropagator.Application.Feeders;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace ThunderPropagator.Channels.NetworkMonitoring
{
    internal
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannelFeeder : IterativeFeeder<NetworkMonitoringChannel, NetworkMonitoringChannelFeederMessage, NetworkMonitoringChannelFeederConfiguration>
    {
        private long _lastBytesReceived;
        private long _lastBytesSent;

        public NetworkMonitoringChannelFeeder(NetworkMonitoringChannel channel,
            NetworkMonitoringChannelFeederConfiguration feederConfiguration,
            IFeederHandler<NetworkMonitoringChannel, NetworkMonitoringChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            HealthName = nameof(NetworkMonitoringChannelFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<NetworkMonitoringChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

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

            var networkMonitoringChannelFeederMessage = new NetworkMonitoringChannelFeederMessage
            {
                DateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                BytesReceived = bytesReceived - _lastBytesReceived,
                BytesSent = bytesSent - _lastBytesSent
            };

            _lastBytesReceived = bytesReceived;
            _lastBytesSent = bytesSent;

            yield return networkMonitoringChannelFeederMessage;
        }
    }
}