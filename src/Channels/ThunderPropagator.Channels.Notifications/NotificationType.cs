namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// The content format of a notification's <see cref="NotificationsChannelFeederMessage.Body"/>
    /// (and <see cref="NotificationsChannelFeederMessage.EllipsisBody"/>). Purely descriptive — this
    /// package does not render, sanitize, or otherwise interpret the body differently based on this
    /// value; a consuming client is responsible for treating <see cref="Html"/> content safely
    /// (e.g. sanitizing before rendering) when it chooses to render it as markup.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>Plain text body. The default when unset.</summary>
        Text,

        /// <summary>Body contains HTML markup intended for rendering by the consuming client.</summary>
        Html
    }
}