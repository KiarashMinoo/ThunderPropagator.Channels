using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Playlist
{
    /// <summary>
    /// Validates one <see cref="VideoPlaylistEntry"/>'s own <see cref="VideoPlaylistEntry.Source"/>
    /// against a <see cref="VideoPlaylistPolicy"/> — #233's own scope. Kept separate from
    /// <see cref="InMemoryVideoPlaylist"/> so this logic is independently testable without constructing
    /// a whole playlist per case.
    /// </summary>
    /// <remarks>
    /// <b>Local vs. remote classification:</b> <see cref="Uri.TryCreate(string?,UriKind,out Uri?)"/> with
    /// <see cref="UriKind.Absolute"/> is used to distinguish the two — empirically (verified directly, not
    /// assumed), a bare Windows drive-letter path like <c>C:\videos\movie.mp4</c> already parses
    /// successfully as an absolute URI with scheme <c>"file"</c> (a real, documented .NET
    /// <see cref="Uri"/> behavior for drive-letter paths), while a Unix-style absolute path
    /// (<c>/srv/videos/movie.mp4</c>), a relative path, or any traversal string
    /// (<c>../../../etc/passwd</c>) all fail to parse as an absolute URI at all. So: a location that
    /// parses as an absolute URI with a <c>"file"</c> scheme, or that fails to parse as an absolute URI
    /// at all, is treated as a local path; anything else that parses as an absolute URI with some other
    /// scheme is treated as remote.
    /// <para/>
    /// <b>Traversal defense:</b> local paths are validated by resolving both the configured root and the
    /// entry's own path through <see cref="Path.GetFullPath(string)"/> (which correctly collapses
    /// <c>..</c>/<c>.</c> segments — well-established .NET behavior, not hand-rolled here) and then
    /// checking containment with a proper path-segment boundary (never a naive
    /// <see cref="string.StartsWith(string)"/> alone, which would wrongly let a sibling directory like
    /// <c>C:\media\approved-evil</c> pass a check against root <c>C:\media\approved</c>).
    /// <para/>
    /// <b>SSRF defense:</b> <see cref="VideoPlaylistPolicy.AllowedRemoteHosts"/> is the primary defense
    /// (deny-by-default — empty means no remote source is ever approved). As a secondary,
    /// defense-in-depth backstop even for an allow-listed host, a host that is itself a literal IP
    /// address is rejected if that address is loopback, a private range (RFC 1918), or link-local
    /// (which includes the well-known cloud metadata endpoint <c>169.254.169.254</c>) — covering both
    /// IPv4 and IPv6.
    /// </remarks>
    public static class VideoPlaylistEntryValidator
    {
        /// <exception cref="VideoPlaylistValidationException">The entry's own source does not satisfy <paramref name="policy"/>.</exception>
        public static void Validate(VideoPlaylistEntry entry, VideoPlaylistPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(policy);

            var location = entry.Source.Location;

            if (Uri.TryCreate(location, UriKind.Absolute, out var uri) && !string.Equals(uri.Scheme, "file", StringComparison.OrdinalIgnoreCase))
            {
                ValidateRemote(uri, policy);
                return;
            }

            // Either a "file" URI (uri.LocalPath is the clean path) or something that never parsed as an
            // absolute URI at all (a Unix-style absolute path, a relative path, or a traversal string) —
            // both are treated as a bare local path, validated the same way.
            var localPath = uri is not null ? uri.LocalPath : location;
            ValidateLocal(localPath, policy);
        }

        private static void ValidateLocal(string localPath, VideoPlaylistPolicy policy)
        {
            if (!policy.AllowedSchemes.Contains("file"))
                throw new VideoPlaylistValidationException("Local file sources are not permitted by the configured policy.");

            if (string.IsNullOrWhiteSpace(policy.LocalFileRoot))
                throw new VideoPlaylistValidationException("No local file root is configured — no local file source can ever be approved.");

            var fullRoot = Path.GetFullPath(policy.LocalFileRoot);
            var fullEntry = Path.GetFullPath(localPath, fullRoot);

            var rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar) ? fullRoot : fullRoot + Path.DirectorySeparatorChar;

            var isWithinRoot = fullEntry.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fullEntry, fullRoot, StringComparison.OrdinalIgnoreCase);

            if (!isWithinRoot)
                throw new VideoPlaylistValidationException("Local file source resolves outside the configured local file root.");
        }

        private static void ValidateRemote(Uri uri, VideoPlaylistPolicy policy)
        {
            if (!policy.AllowedSchemes.Contains(uri.Scheme))
                throw new VideoPlaylistValidationException("Source scheme is not permitted by the configured policy.");

            if (!policy.AllowedRemoteHosts.Contains(uri.Host))
                throw new VideoPlaylistValidationException("Source host is not in the configured remote host allow-list.");

            // Secondary, defense-in-depth backstop — see this type's own remarks. Runs even for an
            // allow-listed host, since the allow-list is matched by hostname string, not by resolved
            // address, and an operator could allow-list a literal IP directly by mistake.
            if (IPAddress.TryParse(uri.Host, out var address) && IsPrivateOrLinkLocalOrLoopback(address))
                throw new VideoPlaylistValidationException("Source host resolves to a private, loopback, or link-local address.");
        }

        private static bool IsPrivateOrLinkLocalOrLoopback(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
                return true;

            var bytes = address.GetAddressBytes();

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // 10.0.0.0/8
                if (bytes[0] == 10)
                    return true;

                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                    return true;

                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;

                // 169.254.0.0/16 — link-local, includes the cloud metadata endpoint 169.254.169.254
                if (bytes[0] == 169 && bytes[1] == 254)
                    return true;

                return false;
            }

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // fc00::/7 — unique local
                if ((bytes[0] & 0xFE) == 0xFC)
                    return true;

                // fe80::/10 — link-local
                if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
                    return true;

                return false;
            }

            return false;
        }
    }
}
