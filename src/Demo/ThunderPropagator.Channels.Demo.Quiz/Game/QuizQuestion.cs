using ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions;
using ThunderPropagator.Channels.Demo.Quiz.Messages;

namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// One quiz question: its text, answer options, and which option is correct. Validated eagerly
    /// in the constructor, so a <see cref="QuizQuestion"/> instance is never in a state a client could
    /// exploit (an out-of-range or ambiguous correct answer) — see #188's own AC.
    /// <see cref="CorrectOptionIndex"/> is never exposed on the wire directly; see
    /// <see cref="QuizChannelFeederMessage.CorrectAnswer"/>'s own phase-gated redaction for how a
    /// question's answer only becomes visible to clients once the game loop reveals it.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizQuestion
    {
        /// <summary>Minimum number of answer choices a question must offer — fewer wouldn't be a meaningful multiple-choice question.</summary>
        public const int MinimumOptionCount = 2;

        public QuizQuestion(string text, IReadOnlyList<string> options, int correctOptionIndex)
        {
            Text = ValidateText(text);
            Options = ValidateOptions(options);
            CorrectOptionIndex = ValidateCorrectOptionIndex(correctOptionIndex, Options);
        }

        public string Text { get; }
        public IReadOnlyList<string> Options { get; }

        /// <summary>Index into <see cref="Options"/> identifying the single correct answer.</summary>
        public int CorrectOptionIndex { get; }

        public string CorrectAnswer => Options[CorrectOptionIndex];

        private static string ValidateText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new QuizQuestionValidationException("question text must not be null, empty, or whitespace-only.");

            return text;
        }

        private static IReadOnlyList<string> ValidateOptions(IReadOnlyList<string> options)
        {
            if (options is null || options.Count < MinimumOptionCount)
                throw new QuizQuestionValidationException($"a question must offer at least {MinimumOptionCount} options.");

            if (options.Any(string.IsNullOrWhiteSpace))
                throw new QuizQuestionValidationException("an option must not be null, empty, or whitespace-only.");

            // Duplicate option text would make "the" correct answer ambiguous to whoever reads it,
            // even though CorrectOptionIndex itself always names exactly one index.
            if (options.Distinct(StringComparer.Ordinal).Count() != options.Count)
                throw new QuizQuestionValidationException("options must not contain duplicates.");

            return options;
        }

        private static int ValidateCorrectOptionIndex(int correctOptionIndex, IReadOnlyList<string> options)
        {
            if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
                throw new QuizQuestionValidationException($"correctOptionIndex must be a valid index into options (0-{options.Count - 1}), was {correctOptionIndex}.");

            return correctOptionIndex;
        }
    }
}
