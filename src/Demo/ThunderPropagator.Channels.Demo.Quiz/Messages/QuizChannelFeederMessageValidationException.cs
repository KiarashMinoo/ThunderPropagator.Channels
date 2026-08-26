using System.Net;
using ThunderPropagator.Channels.Demo.Quiz.Messages;

namespace ThunderPropagator.Channels.Demo.Quiz.Messages
{
    /// <summary>
    /// Thrown when a <see cref="QuizChannelFeederMessage"/> field fails validation — either an
    /// explicit value violates a rule (null, empty, whitespace-only, negative, or over the field's
    /// maximum length/count), or a collection field (<see cref="QuizChannelFeederMessage.Options"/>/
    /// <see cref="QuizChannelFeederMessage.Scoreboard"/>) holds too many entries or an invalid one.
    /// Mirrors NotificationsChannelFeederMessageValidationException's shape (#68/#74).
    /// <see cref="PropertyName"/> identifies which field.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class QuizChannelFeederMessageValidationException(string propertyName, string rule)
        : HttpRequestException($"{propertyName} {rule}", null, HttpStatusCode.NotAcceptable)
    {
        /// <summary>Name of the property that failed validation.</summary>
        public string PropertyName { get; } = propertyName;
    }
}
