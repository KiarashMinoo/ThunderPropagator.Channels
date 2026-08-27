using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Playlist;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Playlist
{
    /// <summary>
    /// Issue #233's own security AC: traversal, arbitrary schemes, and SSRF-style remote input are all
    /// rejected at playlist-construction time, and a rejection's own exception message never echoes the
    /// rejected location back.
    /// </summary>
    public sealed class VideoPlaylistEntryValidatorTests
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "vpl-tests", "approved");

        private static VideoPlaylistEntry Entry(string location) => new()
        {
            VideoId = "video-1",
            Title = "Title",
            Source = new VideoSource { Location = location },
            IsEnabled = true
        };

        private static void AssertAccepted(string location, VideoPlaylistPolicy policy)
        {
            var exception = Record.Exception(() => VideoPlaylistEntryValidator.Validate(Entry(location), policy));
            Assert.Null(exception);
        }

        private static VideoPlaylistValidationException AssertRejected(string location, VideoPlaylistPolicy policy)
        {
            return Assert.Throws<VideoPlaylistValidationException>(() => VideoPlaylistEntryValidator.Validate(Entry(location), policy));
        }

        // --- Local file: acceptance ---

        [Fact]
        public void LocalPath_ResolvesInsideRoot_Accepted()
        {
            var policy = new VideoPlaylistPolicy { LocalFileRoot = Root };
            AssertAccepted(Path.Combine(Root, "movie.mp4"), policy);
        }

        [Fact]
        public void LocalPath_TraversesOutAndBackIn_StillResolvesInsideRoot_Accepted()
        {
            // The final, fully-normalized path is what matters, not a naive scan for ".." substrings —
            // this legitimately resolves back inside the root and must be accepted.
            var policy = new VideoPlaylistPolicy { LocalFileRoot = Root };
            AssertAccepted(Path.Combine(Root, "subdir", "..", "movie.mp4"), policy);
        }

        [Fact]
        public void LocalPath_EqualToRootItself_Accepted()
        {
            var policy = new VideoPlaylistPolicy { LocalFileRoot = Root };
            AssertAccepted(Root, policy);
        }

        // --- Local file: rejection ---

        [Fact]
        public void LocalPath_RelativeTraversalEscapingRoot_Rejected()
        {
            var policy = new VideoPlaylistPolicy { LocalFileRoot = Root };
            AssertRejected(Path.Combine(Root, "..", "..", "..", "etc", "passwd"), policy);
        }

        [Fact]
        public void LocalPath_AbsolutePathEntirelyOutsideRoot_Rejected()
        {
            var policy = new VideoPlaylistPolicy { LocalFileRoot = Root };
            AssertRejected(Path.Combine(Path.GetPathRoot(Root)!, "Windows", "System32", "config", "SAM"), policy);
        }

        [Fact]
        public void LocalPath_SiblingDirectoryWithSimilarPrefix_Rejected()
        {
            // The specific case a naive StartsWith (without a path-separator boundary check) gets wrong.
            var policy = new VideoPlaylistPolicy { LocalFileRoot = Root };
            var evilSibling = Root + "-evil";
            AssertRejected(Path.Combine(evilSibling, "movie.mp4"), policy);
        }

        [Fact]
        public void LocalPath_NoRootConfigured_Rejected()
        {
            var policy = new VideoPlaylistPolicy(); // LocalFileRoot left null
            AssertRejected(Path.Combine(Root, "movie.mp4"), policy);
        }

        [Fact]
        public void LocalPath_FileSchemeNotInAllowedSchemes_Rejected()
        {
            var policy = new VideoPlaylistPolicy { AllowedSchemes = new HashSet<string>(), LocalFileRoot = Root };
            AssertRejected(Path.Combine(Root, "movie.mp4"), policy);
        }

        [Fact]
        public void TraversalRejection_ExceptionMessage_NeverContainsTheRejectedLocation()
        {
            var policy = new VideoPlaylistPolicy { LocalFileRoot = Root };
            var exception = AssertRejected(Path.Combine(Root, "..", "..", "..", "etc", "passwd"), policy);

            Assert.DoesNotContain("etc", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("passwd", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        // --- Remote: scheme/host allow-listing ---

        [Fact]
        public void RemoteUrl_SchemeNotAllowed_Rejected()
        {
            var policy = new VideoPlaylistPolicy
            {
                AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "file" },
                AllowedRemoteHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "example.com" }
            };
            AssertRejected("ftp://example.com/movie.mp4", policy);
        }

        [Fact]
        public void RemoteUrl_JavascriptScheme_Rejected()
        {
            var policy = new VideoPlaylistPolicy();
            AssertRejected("javascript:alert(1)", policy);
        }

        [Fact]
        public void RemoteUrl_HostNotInAllowList_Rejected_EvenWithAllowedScheme()
        {
            var policy = new VideoPlaylistPolicy
            {
                AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "file", "https" },
                AllowedRemoteHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "example.com" }
            };
            AssertRejected("https://not-example.com/movie.mp4", policy);
        }

        [Fact]
        public void RemoteUrl_HostInAllowList_Accepted()
        {
            var policy = new VideoPlaylistPolicy
            {
                AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "file", "https" },
                AllowedRemoteHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "example.com" }
            };
            AssertAccepted("https://example.com/movie.mp4", policy);
        }

        [Fact]
        public void RemoteUrl_NoAllowedHostsConfigured_Rejected_EvenWithAllowedScheme()
        {
            // Deny-by-default: an allowed scheme alone never approves a remote source.
            var policy = new VideoPlaylistPolicy { AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "file", "https" } };
            AssertRejected("https://example.com/movie.mp4", policy);
        }

        // --- Remote: SSRF-style literal-IP backstop, even when the host string is allow-listed ---

        [Theory]
        [InlineData("http://127.0.0.1/movie.mp4")]
        [InlineData("http://10.0.0.5/movie.mp4")]
        [InlineData("http://172.16.0.1/movie.mp4")]
        [InlineData("http://192.168.1.1/movie.mp4")]
        [InlineData("http://169.254.169.254/latest/meta-data/")] // cloud metadata endpoint
        [InlineData("http://[::1]/movie.mp4")]
        [InlineData("http://[fe80::1]/movie.mp4")]
        [InlineData("http://[fc00::1]/movie.mp4")]
        public void RemoteUrl_PrivateOrLoopbackOrLinkLocalIp_Rejected_EvenIfHostStringAllowListed(string location)
        {
            var uri = new Uri(location);
            var policy = new VideoPlaylistPolicy
            {
                AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "file", "http" },
                AllowedRemoteHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { uri.Host }
            };

            AssertRejected(location, policy);
        }

        [Fact]
        public void RemoteUrl_PublicIpLiteral_NotBlockedByThePrivateRangeBackstop()
        {
            // A public IP literal (documentation range TEST-NET-1, RFC 5737 — never routable, safe to
            // use as a "not private" example in a test) allow-listed by host string is accepted; proves
            // the backstop only rejects the specific private/loopback/link-local ranges, not every literal IP.
            const string location = "http://203.0.113.10/movie.mp4";
            var policy = new VideoPlaylistPolicy
            {
                AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "file", "http" },
                AllowedRemoteHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "203.0.113.10" }
            };

            AssertAccepted(location, policy);
        }
    }
}
