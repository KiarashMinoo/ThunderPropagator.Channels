using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.UnitTests.Feeders
{
    /// <summary>
    /// Feeders that gate polling work on subscription state track it themselves via the channel's
    /// public SubscriptionAdded/SubscriptionRemoved events (see e.g. NowClockFeeder) — there's no other
    /// way to observe subscription state from feeder code. AbstractChannel raises those events by
    /// invoking its own field-backed delegate from OnSubscriptionAdded/OnSubscriptionRemoved, which are
    /// only reachable through the real Subscribe/Unsubscribe flow (itself requiring a fully valid
    /// subscribe request, connection info, and channel program descriptors). Reflecting into the
    /// compiler-generated backing field and invoking it directly raises the exact same event a real
    /// subscribe/unsubscribe would, without needing to build that whole request graph.
    /// </summary>
    internal static class ChannelSubscriptionTestHelper
    {
        public static void RaiseSubscriptionAdded(IChannel channel) => RaiseEvent(channel, nameof(IChannel.SubscriptionAdded));

        public static void RaiseSubscriptionRemoved(IChannel channel) => RaiseEvent(channel, nameof(IChannel.SubscriptionRemoved));

        private static void RaiseEvent(IChannel channel, string eventFieldName)
        {
            var field = typeof(AbstractChannel).GetField(eventFieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException(typeof(AbstractChannel).FullName, eventFieldName);

            var multicastDelegate = (MulticastDelegate?)field.GetValue(channel);
            multicastDelegate?.DynamicInvoke(channel, null);
        }
    }
}
