namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>One Opus packet <see cref="AudioFrameEncoder.Encode"/>/<see cref="AudioFrameEncoder.Flush"/> produced, ready to carry as an <see cref="AudioFramePacket.Payload"/>.</summary>
    public readonly record struct EncodedAudioChunk(ReadOnlyMemory<byte> Payload, TimeSpan PresentationTimestamp, TimeSpan Duration);
}
