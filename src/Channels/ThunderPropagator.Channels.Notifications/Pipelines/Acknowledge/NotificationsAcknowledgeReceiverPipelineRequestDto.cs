using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Notifications.Pipelines.Acknowledge
{
    /// <summary>Wire request for <see cref="NotificationsAcknowledgeReceiverPipeline{T}"/> (see #77).</summary>
    internal
#if !DEBUG
        sealed
#endif
        class NotificationsAcknowledgeReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        /// <summary>The <see cref="NotificationsChannelFeederMessage.Id"/> of the notification to acknowledge.</summary>
        public required string Id
        {
            get => (string)this[nameof(Id)];
            set => this[nameof(Id)] = value;
        }

        /// <summary>
        /// The delivery-state flag(s) to acknowledge. UserId is deliberately not a field here — the
        /// pipeline resolves it from the calling connection's own established identity rather than
        /// trusting one supplied over the wire (see <see cref="NotificationsAcknowledgeReceiverPipeline{T}"/>).
        /// </summary>
        public required NotificationDeliveryState State
        {
            get => (NotificationDeliveryState)this[nameof(State)];
            set => this[nameof(State)] = value;
        }
    }
}
