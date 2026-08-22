namespace ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="QuizQuestion"/> or <see cref="QuizQuestionBank"/> when a question, or the
    /// bank as a whole, fails validation — malformed built-in seed data is a programming error to
    /// catch at startup, not something a caller can recover from at runtime.
    /// </summary>
    public sealed class QuizQuestionValidationException(string rule) : Exception($"Invalid quiz question: {rule}");
}
