namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// The raw pixel layout of a <see cref="DecodedVideoFrame"/>'s <see cref="DecodedVideoFrame.Data"/>
    /// — the format a decoder hands off, before a later encoding stage (#217) compresses it into a
    /// <see cref="VideoFramePacketEncoding"/> for the wire. Not itself a wire format.
    /// </summary>
    public enum VideoPixelFormat
    {
        /// <summary>8 bits per channel, red-green-blue, no padding.</summary>
        Rgb24,

        /// <summary>8 bits per channel, blue-green-red, no padding.</summary>
        Bgr24,

        /// <summary>8 bits per channel, red-green-blue-alpha.</summary>
        Rgba32,

        /// <summary>8 bits per channel, blue-green-red-alpha.</summary>
        Bgra32,

        /// <summary>Planar YUV 4:2:0 — the common decode-native format for most compressed video sources.</summary>
        Yuv420P,

        /// <summary>Semi-planar YUV 4:2:0 (one luma plane, one interleaved chroma plane) — the common hardware-decoder-native format.</summary>
        Nv12
    }
}
