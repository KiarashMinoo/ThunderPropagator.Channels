using System.Net;
using ThunderPropagator.Channels.Notifications.Channel;

namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Thrown by <see cref="NotificationsChannel{T}.AcknowledgeAsync"/> (see #77) when no stored,
    /// unexpired notification matches the given <see cref="UserId"/>/<see cref="Id"/> pair.
    /// Deliberately used for both a genuinely unknown Id, an expired one (see #73's ExpiresAt), and
    /// an Id that belongs to a different recipient than <see cref="UserId"/> claims — collapsing
    /// "doesn't exist" and "isn't yours" into the same outcome avoids confirming to an unauthorized
    /// caller that a notification exists for someone else.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class NotificationsChannelUnknownNotificationException(string userId, string id)
        : HttpRequestException($"No unexpired notification found for UserId '{userId}' and Id '{id}'.", null, HttpStatusCode.NotFound)
    {
        /// <summary>The UserId the acknowledgement was attempted against.</summary>
        public string UserId { get; } = userId;

        /// <summary>The Id the acknowledgement was attempted against.</summary>
        public string Id { get; } = id;
    }
}
