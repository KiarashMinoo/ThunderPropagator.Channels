using System.Xml.Linq;
using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #67: the Notifications package's public API lacked XML documentation, leaving NuGet
    /// consumers without IntelliSense guidance. GenerateDocumentationFile is already enabled
    /// repo-wide; this package additionally promotes CS1591 (missing XML comment on a publicly
    /// visible member) from suppressed to a build error in its own csproj, so the compiler itself
    /// guarantees full coverage on every build going forward. This test spot-checks that the
    /// generated XML file actually exists alongside the assembly and carries non-empty content for
    /// a representative sample of the public surface, rather than re-deriving every XML doc-comment
    /// ID via reflection (fragile to get exactly right for generics and overloads).
    /// </summary>
    public sealed class NotificationsXmlDocumentationTests
    {
        [Fact]
        public void GeneratedXmlDocumentationFile_ExistsAndDescribesTheDocumentedPublicSurface()
        {
            var assembly = typeof(NotificationsChannel<>).Assembly;
            var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");

            Assert.True(File.Exists(xmlPath), $"Expected a generated XML documentation file at {xmlPath}.");

            var document = XDocument.Load(xmlPath);
            var members = document.Descendants("member").ToList();

            string[] expectedMemberNames =
            [
                "T:ThunderPropagator.Channels.Notifications.NotificationsChannel`1",
                "T:ThunderPropagator.Channels.Notifications.NotificationsChannelFeederMessage",
                "T:ThunderPropagator.Channels.Notifications.NotificationsChannelMetadata`1",
                "T:ThunderPropagator.Channels.Notifications.NotificationsFeederConfiguration",
                "T:ThunderPropagator.Channels.Notifications.NotificationsExtensions",
                "T:ThunderPropagator.Channels.Notifications.NotificationsHistoricalDateRangeFilter",
                "T:ThunderPropagator.Channels.Notifications.NotificationType",
                "T:ThunderPropagator.Channels.Notifications.NotificationPriority",
                "P:ThunderPropagator.Channels.Notifications.NotificationsChannelFeederMessage.UserId",
                "P:ThunderPropagator.Channels.Notifications.NotificationsFeederConfiguration.BatchSize",
                "F:ThunderPropagator.Channels.Notifications.NotificationsChannelFeederMessage.EllipsisBodyThreshold",
            ];

            foreach (var expectedMemberName in expectedMemberNames)
            {
                var member = members.FirstOrDefault(m => m.Attribute("name")?.Value == expectedMemberName);
                Assert.True(member is not null, $"No <member name=\"{expectedMemberName}\"> entry found in the generated XML documentation.");

                var summary = member!.Element("summary")?.Value.Trim();
                Assert.False(string.IsNullOrWhiteSpace(summary), $"{expectedMemberName} has a <member> entry but no non-empty <summary>.");
            }
        }
    }
}
