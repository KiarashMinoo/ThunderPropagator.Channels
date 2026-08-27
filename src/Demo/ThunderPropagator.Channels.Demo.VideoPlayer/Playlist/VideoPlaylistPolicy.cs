namespace ThunderPropagator.Channels.Demo.VideoPlayer.Playlist
{
    /// <summary>
    /// The server-operator-configured rules every <see cref="VideoPlaylistEntry.Source"/> must satisfy
    /// before <see cref="InMemoryVideoPlaylist"/> will ever construct with it — #233's own scope,
    /// "Validate... scheme/location policy... at startup." This guards against a misconfigured (or
    /// maliciously supplied) playlist entry itself, not against a client — a client can never supply a
    /// path/URL at all (see #228's own scope notes), so this is a startup/configuration-time defense,
    /// not a wire-facing one.
    /// </summary>
    /// <remarks>
    /// Deny-by-default throughout: an empty <see cref="AllowedRemoteHosts"/> (the default) means no
    /// remote source is ever approved regardless of <see cref="AllowedSchemes"/>, and a
    /// <see langword="null"/> <see cref="LocalFileRoot"/> (the default) means no local file is ever
    /// approved either — a policy approves nothing until explicitly configured to approve something,
    /// which is the correct default posture for a security boundary like this one.
    /// </remarks>
    public sealed record VideoPlaylistPolicy
    {
        /// <summary>
        /// Schemes an entry's <see cref="VideoPlaylistEntry.Source"/> location may use. <c>"file"</c> (or
        /// no scheme at all — a bare local path) is always validated against <see cref="LocalFileRoot"/>
        /// regardless of whether it is explicitly listed here; every other scheme listed here is validated
        /// against <see cref="AllowedRemoteHosts"/>. Defaults to only <c>"file"</c>.
        /// </summary>
        public IReadOnlySet<string> AllowedSchemes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "file" };

        /// <summary>
        /// The directory every local-file entry must resolve inside — required (non-null, non-whitespace)
        /// for any local-file entry to ever validate successfully. <see langword="null"/> (the default)
        /// means no local file is ever approved, regardless of <see cref="AllowedSchemes"/>.
        /// </summary>
        public string? LocalFileRoot { get; init; }

        /// <summary>
        /// Explicit host allow-list for any non-<c>"file"</c> scheme — the primary SSRF defense
        /// (deny-by-default: an empty set, the default, means no remote source is ever approved). A
        /// literal-private-IP check (see <see cref="VideoPlaylistEntryValidator"/>'s own remarks) is a
        /// secondary, defense-in-depth backstop for when a host here happens to be (or resolves as) a
        /// private/loopback/link-local address, such as a cloud metadata endpoint.
        /// </summary>
        public IReadOnlySet<string> AllowedRemoteHosts { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
