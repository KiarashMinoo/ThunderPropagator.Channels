namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// The codec an <see cref="AudioFramePacket.Payload"/> is compressed with. Values are explicit and
    /// stable on the wire (see <see cref="AudioFramePacketSerializer"/>) so a new codec can be appended
    /// without renumbering existing ones, mirroring <see cref="VideoFramePacketEncoding"/>'s own scheme
    /// — #224's own scope: "Decode and encode audio server-side using a negotiated browser/client-
    /// supported codec." Opus is the only codec this server currently negotiates/produces — universally
    /// supported by browsers via WebCodecs/MediaSource, low-latency, and purpose-built for streaming.
    /// </summary>
    public enum AudioFramePacketEncoding : byte
    {
        /// <summary>The Opus codec (RFC 6716).</summary>
        Opus = 0
    }
}
