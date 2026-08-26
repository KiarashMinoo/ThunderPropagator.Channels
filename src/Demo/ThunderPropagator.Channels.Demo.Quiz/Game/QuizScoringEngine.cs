using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Messages;

namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// Server-authoritative scoring for one game: which answer is correct for the currently open
    /// question, who has already answered it, and each player's cumulative score across the whole
    /// game. A client only ever supplies which option it picked — never a score — so nothing here can
    /// be influenced by anything other than <see cref="QuizGameLoop"/> itself calling
    /// <see cref="BeginQuestion"/>/<see cref="CloseQuestion"/>/<see cref="SubmitAnswer"/> at the right
    /// moments (#190's own AC: "Keep scoring state server-authoritative"). Thread-safe on its own
    /// merit, independent of whatever locking its caller does, since a real deployment's answer
    /// submissions (a future #192 pipeline) arrive on arbitrary request threads while the game loop
    /// itself keeps ticking concurrently on its own.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizScoringEngine
    {
        /// <summary>Points awarded for a correct answer regardless of timing.</summary>
        public const int CorrectAnswerBasePoints = 1000;

        /// <summary>
        /// Maximum additional points awarded for answering immediately — scaled linearly down to zero
        /// as the question's countdown runs out. #190's own AC: "a deterministic scoring rule,
        /// including optional response-time bonus."
        /// </summary>
        public const int MaxResponseTimeBonus = 500;

#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif

        private readonly Dictionary<string, int> _scoresByPlayerName = new(StringComparer.Ordinal);
        private readonly HashSet<string> _answeredPlayerNames = new(StringComparer.Ordinal);

        private bool _questionIsOpen;
        private string _correctAnswer = string.Empty;
        private TimeSpan _questionDuration;

        /// <summary>
        /// Opens a fresh answer window for a new question: <paramref name="correctAnswer"/> is what
        /// <see cref="SubmitAnswer"/> compares a submission against, and <paramref name="questionDuration"/>
        /// is the full window length the response-time bonus is scaled against. Forgets which players
        /// answered the previous question — the AC's "at most one answer per player per question" is
        /// scoped to a single question, not the whole game.
        /// </summary>
        public void BeginQuestion(string correctAnswer, TimeSpan questionDuration)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(correctAnswer);

            lock (_lock)
            {
                _correctAnswer = correctAnswer;
                _questionDuration = questionDuration;
                _answeredPlayerNames.Clear();
                _questionIsOpen = true;
            }
        }

        /// <summary>
        /// Closes the current question's answer window — every call to <see cref="SubmitAnswer"/> from
        /// this point on returns <see cref="QuizAnswerOutcome.WindowClosed"/> until the next
        /// <see cref="BeginQuestion"/>. #190's own AC: "Evaluate only answers accepted within the
        /// active question window."
        /// </summary>
        public void CloseQuestion()
        {
            lock (_lock)
            {
                _questionIsOpen = false;
            }
        }

        /// <summary>
        /// Records <paramref name="playerName"/>'s answer for the currently open question, if any is
        /// open and this is their first submission for it. A correct answer earns
        /// <see cref="CorrectAnswerBasePoints"/> plus a response-time bonus of up to
        /// <see cref="MaxResponseTimeBonus"/>, scaled by how much of the question's countdown
        /// (<paramref name="timeRemaining"/> out of the duration passed to <see cref="BeginQuestion"/>)
        /// was still left when this was called — answering the instant the question opens earns the
        /// full bonus, answering as it's about to close earns none. An incorrect answer, once accepted,
        /// still consumes this player's one answer for the question (#190's own AC: "Accept at most one
        /// effective answer per player per question") — there is no way to revise a wrong answer into a
        /// right one by submitting again.
        /// </summary>
        public QuizAnswerOutcome SubmitAnswer(string playerName, string selectedOption, TimeSpan timeRemaining)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(playerName);

            lock (_lock)
            {
                if (!_questionIsOpen)
                    return QuizAnswerOutcome.WindowClosed;

                if (!_answeredPlayerNames.Add(playerName))
                    return QuizAnswerOutcome.Duplicate;

                if (!string.Equals(selectedOption, _correctAnswer, StringComparison.Ordinal))
                    return QuizAnswerOutcome.Incorrect;

                var fractionRemaining = _questionDuration > TimeSpan.Zero
                    ? Math.Clamp(timeRemaining / _questionDuration, 0d, 1d)
                    : 0d;
                var bonus = (int)Math.Round(fractionRemaining * MaxResponseTimeBonus);

                _scoresByPlayerName[playerName] = _scoresByPlayerName.GetValueOrDefault(playerName) + CorrectAnswerBasePoints + bonus;
                return QuizAnswerOutcome.Correct;
            }
        }

        /// <summary>
        /// Every player who has ever scored at least once, sorted highest score first — score ties
        /// broken by player name, ordinal ascending, so the ordering is fully deterministic regardless
        /// of submission order (#190's own AC: "Scoreboard ordering and tie-breaking are deterministic
        /// and documented"). A player who has answered but never scored is not listed — matching
        /// <see cref="QuizChannelFeederMessage.Scoreboard"/>'s own docs ("empty until at least one
        /// question has been scored").
        /// </summary>
        public IReadOnlyList<QuizScoreboardEntry> BuildScoreboard()
        {
            lock (_lock)
            {
                return _scoresByPlayerName
                    .Select(entry => new QuizScoreboardEntry(entry.Key, entry.Value))
                    .OrderByDescending(entry => entry.Score)
                    .ThenBy(entry => entry.PlayerName, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <summary>
        /// The name(s) of whoever has the highest score, comma-and-space-joined in ordinal name order
        /// when more than one player is tied for it (#190's own AC: "GameOver exposes the correct
        /// winner or documented tied winners") — e.g. "Alice, Bob" rather than picking one arbitrarily.
        /// Empty if nobody has scored at all.
        /// </summary>
        public string DetermineWinner()
        {
            lock (_lock)
            {
                if (_scoresByPlayerName.Count == 0)
                    return string.Empty;

                var topScore = _scoresByPlayerName.Values.Max();

                return string.Join(", ", _scoresByPlayerName
                    .Where(entry => entry.Value == topScore)
                    .Select(entry => entry.Key)
                    .OrderBy(name => name, StringComparer.Ordinal));
            }
        }
    }
}
