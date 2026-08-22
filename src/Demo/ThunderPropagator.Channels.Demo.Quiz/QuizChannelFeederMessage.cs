using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    // Issue #183: pure scaffold — no fields yet. #186 defines the real serialization contract
    // (question/answer/scoreboard payload shape) this message will carry.
    internal
#if !DEBUG
        sealed
#endif
        class QuizChannelFeederMessage : FeederMessage
    {
    }
}
