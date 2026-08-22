using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    // Issue #185: describes QuizChannelFeederMessage's fields. This ticket owns both the descriptor
    // set AND the feeder-message properties it describes — #186 ("implement the serialization
    // contract") couldn't come first, since descriptors have nothing to nameof() against without
    // real properties, and can't come after without leaving these descriptors dangling; see the
    // feeder message's own comment for the same reasoning stated there.
    public
#if !DEBUG
        sealed
#endif
        class QuizChannelMetadata : AbstractChannelMetadata<QuizChannel>
    {
        public const string QuizGame = nameof(QuizGame);

        /// <summary>
        /// GameId (0) is the only subscribing key — a client subscribes to one game session and
        /// receives every field of it, rather than subscribing per-field the way Chat's per-message
        /// UserId key works. Phase (1) uses the enum descriptor so its underlying int value is never
        /// mistaken for an arbitrary number field; TimeRemaining/QuestionIndex/TotalQuestions (4-6)
        /// use the numeric descriptor per the AC's own "timing/count fields use numeric descriptors".
        /// Options (3) and Scoreboard (7) are JSON-encoded since they're collections, not scalars —
        /// mirroring NotificationsChannelFeederMessage.Tags' own JsonChannelProgramsDescriptor.
        /// CorrectAnswer (8) and Winner (9) are plain strings whose own getters redact them until the
        /// phase that's supposed to reveal them — see QuizChannelFeederMessage's own comments; this
        /// descriptor only declares their wire type, not that redaction rule, which lives entirely in
        /// the property itself and applies regardless of which transport reads it.
        /// </summary>
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new()
        {
            new SubscribingKeyChannelProgramsDescriptor(0, nameof(QuizChannelFeederMessage.GameId), "The game session identifier").SetTable(QuizGame),
            new EnumChannelProgramsDescriptor<QuizPhase>(1, nameof(QuizChannelFeederMessage.Phase), "The game's current lifecycle phase").SetTable(QuizGame),
            new ChannelProgramsDescriptor(2, nameof(QuizChannelFeederMessage.QuestionText), DataType.String, "The current question's text").SetTable(QuizGame),
            new JsonChannelProgramsDescriptor(3, nameof(QuizChannelFeederMessage.Options), "The current question's answer choices").SetTable(QuizGame),
            new NumberChannelProgramsDescriptor(4, nameof(QuizChannelFeederMessage.TimeRemaining), "Seconds remaining in the current question countdown").SetTable(QuizGame),
            new NumberChannelProgramsDescriptor(5, nameof(QuizChannelFeederMessage.QuestionIndex), "0-based index of the current question").SetTable(QuizGame),
            new NumberChannelProgramsDescriptor(6, nameof(QuizChannelFeederMessage.TotalQuestions), "Total number of questions in the game").SetTable(QuizGame),
            new JsonChannelProgramsDescriptor(7, nameof(QuizChannelFeederMessage.Scoreboard), "Current player standings").SetTable(QuizGame),
            new ChannelProgramsDescriptor(8, nameof(QuizChannelFeederMessage.CorrectAnswer), DataType.String, "The correct answer — empty before the Revealing phase").SetTable(QuizGame),
            new ChannelProgramsDescriptor(9, nameof(QuizChannelFeederMessage.Winner), DataType.String, "The winning player's name — empty before GameOver").SetTable(QuizGame)
        };
    }
}
