using System.Net;

namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Thrown when a <see cref="NotificationsChannelFeederMessage"/> field fails validation —
    /// either an explicit value violates a rule (null, empty, whitespace-only, or over the field's
    /// maximum length), or a required field was never set by the time the message reaches the
    /// channel's emission boundary. <see cref="PropertyName"/> identifies which field.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class NotificationsChannelFeederMessageValidationException(string propertyName, string rule)
        : HttpRequestException($"{propertyName} {rule}", null, HttpStatusCode.NotAcceptable)
    {
        /// <summary>Name of the property that failed validation.</summary>
        public string PropertyName { get; } = propertyName;
    }
}
