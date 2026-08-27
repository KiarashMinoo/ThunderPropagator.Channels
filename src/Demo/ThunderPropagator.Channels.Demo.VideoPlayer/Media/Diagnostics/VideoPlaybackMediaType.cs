namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Diagnostics
{
    /// <summary>
    /// Which track a <see cref="VideoPlaybackTelemetry"/> call describes — the one dimension every
    /// per-track instrument is tagged by. A fixed two-value enum rather than a free-form string keeps
    /// that tag bounded, per #235's own AC, "Tag by bounded identifiers; avoid high-cardinality
    /// viewer/frame tags."
    /// </summary>
    public enum VideoPlaybackMediaType
    {
        /// <summary>The video track.</summary>
        Video,

        /// <summary>The audio track.</summary>
        Audio
    }
}
