using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer
{
    /// <summary>
    /// Thrown when a <see cref="VideoPlayerChannelFeederMessage"/> field fails validation — either an
    /// explicit value violates a rule (null/empty/negative/over the field's maximum length/count), or
    /// <see cref="VideoPlayerChannelFeederMessage.ValidateForCurrentState"/> finds a field missing or
    /// inconsistent for the message's current <see cref="VideoPlayerChannelFeederMessage.State"/>.
    /// Mirrors QuizChannelFeederMessageValidationException's shape (#186). <see cref="PropertyName"/>
    /// identifies which field.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class VideoPlayerChannelFeederMessageValidationException(string propertyName, string rule)
        : HttpRequestException($"{propertyName} {rule}", null, HttpStatusCode.NotAcceptable)
    {
        /// <summary>Name of the property that failed validation.</summary>
        public string PropertyName { get; } = propertyName;
    }
}
