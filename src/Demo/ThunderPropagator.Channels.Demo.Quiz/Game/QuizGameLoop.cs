using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// The authoritative, time-driven progression a single <see cref="QuizGameSession"/> follows —
    /// Lobby wait → Question countdown → Revealing pause → Scoreboard pause → the next Question or
    /// GameOver — with no external trigger once it begins (#189's own AC). Deliberately kept free of
    /// any actual waiting: <see cref="NextDelay"/> reports how long the caller should wait before
    /// calling <see cref="Advance"/>, and <see cref="Advance"/> performs exactly one step assuming
    /// that wait already happened. <see cref="QuizFeeder"/> is the only caller that actually awaits
    /// <see cref="NextDelay"/> against a real clock — every rule about what to wait for and what
    /// happens next lives here instead, so it can be tested synchronously with no real elapsed time at
    /// all.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizGameLoop
    {
        private readonly QuizGameSession _session;
        private readonly QuizQuestionBank _questionBank;
        private readonly QuizFeederConfiguration _feederConfiguration;

        private IReadOnlyList<QuizQuestion> _questions = [];
        private int _questionIndex;
        private TimeSpan _questionTimeRemaining;

        public QuizGameLoop(QuizGameSession session, QuizQuestionBank questionBank, QuizFeederConfiguration feederConfiguration)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(questionBank);
            ArgumentNullException.ThrowIfNull(feederConfiguration);

            _session = session;
            _questionBank = questionBank;
            _feederConfiguration = feederConfiguration;
        }

        /// <summary>
        /// How long a caller should wait before the next <see cref="Advance"/> call — the full
        /// configured duration of whichever phase the session is currently in, except
        /// <see cref="QuizPhase.Question"/>, which is capped to at most one second at a time so
        /// <see cref="QuizChannelFeederMessage.TimeRemaining"/> can broadcast a live countdown rather
        /// than jumping straight from the full duration to zero (that property's own docs note it is
        /// meaningful only during Question — every other phase has nothing to count down, hence one
        /// wait for their entire duration). Never <see cref="TimeSpan.Zero"/>, including once
        /// <see cref="QuizPhase.GameOver"/> is reached: <see cref="Advance"/> becomes a no-op from
        /// that point on (#189's own AC only requires reaching GameOver, not restarting afterward),
        /// but a caller must keep waiting a real, positive interval between calls regardless — that is
        /// what keeps polling a finished session from becoming the busy-spin #189's own AC forbids.
        /// </summary>
        public TimeSpan NextDelay => _session.PhaseStateMachine.CurrentPhase switch
        {
            QuizPhase.Lobby => _feederConfiguration.LobbyDuration,
            QuizPhase.Question => QuestionTick,
            QuizPhase.Revealing => _feederConfiguration.RevealingDuration,
            QuizPhase.Scoreboard => _feederConfiguration.ScoreboardDuration,
            _ => _feederConfiguration.ScoreboardDuration
        };

        /// <summary>
        /// Advances this session by exactly one step, assuming <see cref="NextDelay"/> has already
        /// elapsed, records the resulting state as <see cref="QuizGameSession.CurrentState"/> (#187 —
        /// the snapshot a future join pipeline unicasts to a mid-game joiner), and returns the message
        /// representing it. Returns null once <see cref="QuizPhase.GameOver"/> was already reached
        /// before this call: there is nothing left to advance, and nothing new to broadcast.
        /// </summary>
        public QuizChannelFeederMessage? Advance()
        {
            var stateMachine = _session.PhaseStateMachine;

            switch (stateMachine.CurrentPhase)
            {
                case QuizPhase.Lobby:
                    stateMachine.StartGame();
                    _questions = _questionBank.Shuffle(Random.Shared.Next());
                    _questionIndex = 0;
                    _questionTimeRemaining = _feederConfiguration.QuestionDuration;
                    break;

                case QuizPhase.Question:
                    _questionTimeRemaining -= QuestionTick;
                    if (_questionTimeRemaining > TimeSpan.Zero)
                        break;

                    stateMachine.RevealAnswer();
                    break;

                case QuizPhase.Revealing:
                    stateMachine.ShowScoreboard();
                    break;

                case QuizPhase.Scoreboard:
                    var hasMoreQuestions = _questionIndex + 1 < _questions.Count;
                    if (hasMoreQuestions)
                    {
                        _questionIndex++;
                        _questionTimeRemaining = _feederConfiguration.QuestionDuration;
                    }

                    stateMachine.NextQuestion(hasMoreQuestions);
                    break;

                case QuizPhase.GameOver:
                default:
                    return null;
            }

            var message = BuildMessage(stateMachine.CurrentPhase);
            _session.UpdateCurrentState(message);
            return message;
        }

        /// <summary>
        /// The remainder of <see cref="_questionTimeRemaining"/>, capped to at most one second — the
        /// same formula <see cref="NextDelay"/> reports and <see cref="Advance"/> actually consumes, so
        /// the two can never drift apart regardless of how short (e.g. a test's) <see cref="QuizFeederConfiguration.QuestionDuration"/> is configured.
        /// </summary>
        private TimeSpan QuestionTick => _questionTimeRemaining < TimeSpan.FromSeconds(1) ? _questionTimeRemaining : TimeSpan.FromSeconds(1);

        // Scoreboard/Winner are #190's own scope (answer evaluation, scoring, winner selection) — this
        // loop only ever drives phase and question selection, never populates either field, matching
        // the layered, ticket-by-ticket ownership already established by #184/#187/#188.
        private QuizChannelFeederMessage BuildMessage(QuizPhase phase)
        {
            var message = new QuizChannelFeederMessage
            {
                GameId = _session.GameId,
                Phase = phase,
                QuestionIndex = _questionIndex,
                TotalQuestions = _questions.Count,
                TimeRemaining = phase == QuizPhase.Question ? (int)Math.Ceiling(_questionTimeRemaining.TotalSeconds) : 0
            };

            if (phase is QuizPhase.Question or QuizPhase.Revealing)
            {
                var question = _questions[_questionIndex];
                message.QuestionText = question.Text;
                message.Options = question.Options;
                message.CorrectAnswer = question.CorrectAnswer;
            }

            return message;
        }
    }
}
