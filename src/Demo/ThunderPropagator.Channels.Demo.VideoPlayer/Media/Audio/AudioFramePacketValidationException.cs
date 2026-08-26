using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// Thrown by <see cref="AudioFramePacketSerializer"/> when an <see cref="AudioFramePacket"/> field
    /// fails validation on write (a caller-constructed packet violates a bound) or when a decoded packet
    /// fails validation on read (a malformed/corrupted/oversized wire buffer). Mirrors
    /// <see cref="VideoFramePacketValidationException"/>'s own role for the video side.
    /// <see cref="PropertyName"/> identifies which field.
    /// </summary>
    public sealed class AudioFramePacketValidationException(string propertyName, string rule) : Exception($"{propertyName} {rule}")
    {
        /// <summary>Name of the field that failed validation.</summary>
        public string PropertyName { get; } = propertyName;
    }
}
