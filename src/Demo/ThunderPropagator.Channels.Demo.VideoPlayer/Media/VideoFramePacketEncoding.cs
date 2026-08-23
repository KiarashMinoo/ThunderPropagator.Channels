namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// The pixel encoding a <see cref="VideoFramePacket.Payload"/> is compressed with. Values are
    /// explicit and stable on the wire (see <see cref="VideoFramePacketSerializer"/>) so a new encoding
    /// can be appended without renumbering existing ones — #214's own "extensible ... strategy" AC.
    /// Actual encoding negotiation (which encodings a given client/session supports or prefers) is out
    /// of this type's scope; it only names what a packet's payload already is.
    /// </summary>
    public enum VideoFramePacketEncoding : byte
    {
        /// <summary>Baseline/progressive JPEG.</summary>
        Jpeg = 0,

        /// <summary>Lossy or lossless WebP.</summary>
        WebP = 1
    }
}
