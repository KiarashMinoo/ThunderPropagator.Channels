using System.Net;
using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Channel;

namespace ThunderPropagator.Channels.Notifications.Pipelines.Acknowledge
{
    /// <summary>
    /// Thrown by <see cref="NotificationsAcknowledgeReceiverPipeline{T}"/> (see #77) when the calling
    /// connection has never subscribed — and so has no UserId recorded in
    /// <c>NotificationsChannel{T}.SubscribedUserIdsByConnectionId</c> — to acknowledge as. This is a
    /// distinct, earlier rejection than <see cref="NotificationsChannelUnknownNotificationException"/>:
    /// it means the connection has no identity to check against at all, before any lookup against a
    /// specific notification even happens.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class NotificationsAcknowledgeReceiverPipelineUnauthorizedException()
        : HttpRequestException("This connection has not subscribed with a UserId and cannot acknowledge notifications.", null, HttpStatusCode.Unauthorized)
    {
    }
}
