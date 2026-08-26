namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// Thrown by <see cref="VideoFramePacketSerializer"/> when a <see cref="VideoFramePacket"/> field
    /// fails validation on write (a caller-constructed packet violates a bound) or when a decoded
    /// packet fails validation on read (a malformed/corrupted/oversized wire buffer) — #214's own AC:
    /// "Malformed lengths, unsupported encodings, and oversized payloads are rejected."
    /// <see cref="PropertyName"/> identifies which field.
    /// </summary>
    public sealed class VideoFramePacketValidationException(string propertyName, string rule) : Exception($"{propertyName} {rule}")
    {
        /// <summary>Name of the field that failed validation.</summary>
        public string PropertyName { get; } = propertyName;
    }
}
