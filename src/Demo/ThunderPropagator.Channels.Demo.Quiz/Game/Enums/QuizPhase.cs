namespace ThunderPropagator.Channels.Demo.Quiz.Game.Enums
{
    /// <summary>
    /// The quiz game's lifecycle phase. The normal sequence is
    /// <see cref="Lobby"/> → <see cref="Question"/> → <see cref="Revealing"/> → <see cref="Scoreboard"/>
    /// → next <see cref="Question"/> or <see cref="GameOver"/> — see
    /// <see cref="ThunderPropagator.Channels.Demo.Quiz.Game.QuizPhaseStateMachine"/> for the enforced
    /// transition rules.
    /// </summary>
    public enum QuizPhase
    {
        /// <summary>Players are joining and waiting for the host to start. The initial phase, and the phase every game returns to via <c>Restart</c>/<c>Cancel</c>.</summary>
        Lobby,

        /// <summary>A question is live and accepting player answers.</summary>
        Question,

        /// <summary>The correct answer is being shown; no further answers are accepted.</summary>
        Revealing,

        /// <summary>Current standings are shown before the next question or game end.</summary>
        Scoreboard,

        /// <summary>The game has finished; final standings are shown. Only <c>Restart</c> leaves this phase.</summary>
        GameOver
    }
}
