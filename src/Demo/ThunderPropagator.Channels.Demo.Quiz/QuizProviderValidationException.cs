namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Thrown by <see cref="QuizChannel.PublishProviderState"/> when <see cref="QuizProviderPublishRequest.Phase"/>
    /// requires a field the request left empty/incomplete — #194's own AC: "Invalid or incomplete
    /// phase-specific requests fail clearly." Distinct from <see cref="QuizChannelFeederMessageValidationException"/>,
    /// which the same call path also surfaces unchanged for anything the wire message's own property
    /// setters already reject (GameId, text/collection length limits, negative timing) — this one is
    /// specifically for requiredness rules the wire message itself does not enforce, since it legitimately
    /// allows those same fields to be empty at other phases.
    /// </summary>
    public sealed class QuizProviderValidationException(string propertyName, string rule) : Exception($"{propertyName} {rule}")
    {
        public string PropertyName { get; } = propertyName;
    }
}
