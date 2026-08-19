using ThunderPropagator.Application.Collections;

namespace ThunderPropagator.Channels.Notifications.Pipelines.Acknowledge
{
    /// <summary>Wire response for <see cref="NotificationsAcknowledgeReceiverPipeline{T}"/> (see #77).</summary>
    internal
#if !DEBUG
        sealed
#endif
        class NotificationsAcknowledgeReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        /// <summary>
        /// The full delivery-state flags now stored for this notification, after merging in the
        /// acknowledged state — not just the flag(s) this request acknowledged.
        /// </summary>
        public required NotificationDeliveryState State { get; init; }
    }
}
