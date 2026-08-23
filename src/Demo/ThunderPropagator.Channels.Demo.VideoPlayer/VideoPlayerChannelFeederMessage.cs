using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Demo.VideoPlayer
{
    // Issue #213: pure scaffold — no fields yet. #215 defines the real playback-state serialization
    // contract (phase/session/epoch/position) this message will carry; binary frame/audio payloads
    // themselves are never carried here — see #214's own VideoFramePacket transport instead.
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerChannelFeederMessage : FeederMessage
    {
    }
}
