namespace ThunderPropagator.Channels.Demo.VideoPlayer
{
    /// <summary>
    /// A video-player session's current lifecycle state, broadcast on
    /// <see cref="VideoPlayerChannelFeederMessage.State"/>. Unlike a strictly linear phase sequence,
    /// several transitions are legal in more than one direction (e.g. <see cref="Playing"/> ↔
    /// <see cref="Buffering"/> as the network keeps up or falls behind, or any state → <see cref="Faulted"/>)
    /// — the exact transition graph belongs to whichever future ticket owns the session/decoder
    /// lifecycle; this type only names the states themselves.
    /// </summary>
    public enum PlayState
    {
        /// <summary>A video has been selected and is being prepared; no frame has been published yet.</summary>
        Loading,

        /// <summary>Frames are being decoded, paced, and published at their scheduled display times.</summary>
        Playing,

        /// <summary>Playback is intentionally held at its current position; no further frames are published.</summary>
        Paused,

        /// <summary>Playback is temporarily stalled — decode/publish has fallen behind and is catching up — distinct from a host-requested <see cref="Paused"/>.</summary>
        Buffering,

        /// <summary>The video reached its end; no further frames remain for this selection.</summary>
        Ended,

        /// <summary>The session hit an unrecoverable error — the source, decoder, or session itself failed.</summary>
        Faulted
    }
}
