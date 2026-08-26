using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;
namespace ThunderPropagator.Channels.Demo.VideoPlayer
{
    /// <summary>
    /// One reaction type's current aggregate count, as carried by
    /// <see cref="VideoPlayerChannelFeederMessage.Reactions"/>. Aggregated, not a per-event stream —
    /// the parent epic's own remarks describe <c>Video/React</c> as aggregated outside the media hot
    /// path, so this snapshot only ever reports running totals, never individual reaction events.
    /// </summary>
    public sealed record VideoReactionCount(string Reaction, int Count);
}
