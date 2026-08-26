using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// The codec an <see cref="AudioFramePacket.Payload"/> is compressed with. Values are explicit and
    /// stable on the wire (see <see cref="AudioFramePacketSerializer"/>) so a new codec can be appended
    /// without renumbering existing ones, mirroring <see cref="VideoFramePacketEncoding"/>'s own scheme
    /// — #224's own scope: "Decode and encode audio server-side using a negotiated browser/client-
    /// supported codec." A session picks between these — explicitly via
    /// <see cref="VideoPlaybackSessionOptions.AudioEncoding"/>, or automatically from the selected
    /// source's own audio codec otherwise — see <see cref="VideoPlaybackSession"/>'s own remarks on
    /// audio, and <see cref="VideoPlaybackSession.AudioEncoding"/> for what a session actually chose.
    /// Every published <see cref="AudioFramePacket"/> also carries its own <see cref="AudioFramePacket.Encoding"/>,
    /// so a client always knows which codec it is looking at from the packet alone, without needing the
    /// session-level signal at all.
    /// </summary>
    public enum AudioFramePacketEncoding : byte
    {
        /// <summary>The Opus codec (RFC 6716) — universally supported by browsers via WebCodecs/MediaSource, low-latency, purpose-built for streaming. The default choice when nothing else applies.</summary>
        Opus = 0,

        /// <summary>Advanced Audio Coding (MPEG-4 Part 3) — broader compatibility with older browsers/Safari and content already natively encoded as AAC, at the cost of higher latency than Opus.</summary>
        Aac = 1
    }
}
