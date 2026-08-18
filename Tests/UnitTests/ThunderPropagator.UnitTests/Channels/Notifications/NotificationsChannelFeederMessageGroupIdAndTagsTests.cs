using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #74: GroupId and Tags let a message represent a group audience and free-form
    /// categorization beyond the fixed fields that already existed. These tests cover the two
    /// properties themselves — defaults, validation, case-insensitive tag deduplication, and
    /// copy-construction propagation — separately from NotificationsChannelGroupRoutingTests, which
    /// covers how the channel actually routes and filters by them.
    /// </summary>
    public sealed class NotificationsChannelFeederMessageGroupIdAndTagsTests
    {
        [Fact]
        public void GroupId_DefaultsToNull()
        {
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };

            Assert.Null(message.GroupId);
        }

        [Fact]
        public void GroupId_CanBeSetViaObjectInitializer()
        {
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", GroupId = "group-1" };

            Assert.Equal("group-1", message.GroupId);
        }

        [Fact]
        public void CopyConstructor_PropagatesGroupIdToTheCopy()
        {
            var source = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", GroupId = "group-1" };

            var copy = new NotificationsChannelFeederMessage(source) { UserId = "user-1" };

            Assert.Equal("group-1", copy.GroupId);
        }

        [Fact]
        public void Tags_DefaultsToAnEmptyCollection_NeverNull()
        {
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };

            Assert.NotNull(message.Tags);
            Assert.Empty(message.Tags);
        }

        [Fact]
        public void Tags_CanBeSetViaObjectInitializer()
        {
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = ["urgent", "billing"] };

            Assert.Equal(["urgent", "billing"], message.Tags);
        }

        [Fact]
        public void Tags_DeduplicatesCaseInsensitively_KeepingTheFirstOccurrencesCasing()
        {
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = ["Urgent", "urgent", "URGENT", "billing"] };

            Assert.Equal(["Urgent", "billing"], message.Tags);
        }

        [Fact]
        public void Tags_PreservesInsertionOrderOfFirstOccurrences()
        {
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = ["b", "a", "b", "c"] };

            Assert.Equal(["b", "a", "c"], message.Tags);
        }

        [Fact]
        public void Tags_RejectsANullTag()
        {
            var exception = Record.Exception(() => new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = ["valid", null!] });

            Assert.IsType<NotificationsChannelFeederMessageValidationException>(exception);
        }

        [Fact]
        public void Tags_RejectsAWhitespaceOnlyTag()
        {
            var exception = Record.Exception(() => new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = ["valid", "   "] });

            Assert.IsType<NotificationsChannelFeederMessageValidationException>(exception);
        }

        [Fact]
        public void Tags_RejectsATagLongerThanTagMaxLength()
        {
            var tooLong = new string('a', NotificationsChannelFeederMessage.TagMaxLength + 1);

            var exception = Record.Exception(() => new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = [tooLong] });

            Assert.IsType<NotificationsChannelFeederMessageValidationException>(exception);
        }

        [Fact]
        public void Tags_AcceptsATagExactlyAtTagMaxLength()
        {
            var exactLength = new string('a', NotificationsChannelFeederMessage.TagMaxLength);

            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = [exactLength] };

            Assert.Equal([exactLength], message.Tags);
        }

        [Fact]
        public void Tags_RejectsMoreThanTagsMaxCountDistinctTags()
        {
            var tooMany = Enumerable.Range(0, NotificationsChannelFeederMessage.TagsMaxCount + 1).Select(i => $"tag-{i}").ToArray();

            var exception = Record.Exception(() => new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = tooMany });

            Assert.IsType<NotificationsChannelFeederMessageValidationException>(exception);
        }

        [Fact]
        public void Tags_AcceptsExactlyTagsMaxCountDistinctTags()
        {
            var exactCount = Enumerable.Range(0, NotificationsChannelFeederMessage.TagsMaxCount).Select(i => $"tag-{i}").ToArray();

            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = exactCount };

            Assert.Equal(NotificationsChannelFeederMessage.TagsMaxCount, message.Tags.Count);
        }

        [Fact]
        public void Tags_DuplicatesDoNotCountTowardTagsMaxCount()
        {
            // TagsMaxCount duplicates of the same tag, plus one extra distinct tag, should still fit
            // — the cap applies to distinct tags, not raw entries in the assigned collection.
            var mostlyDuplicates = Enumerable.Repeat("same-tag", NotificationsChannelFeederMessage.TagsMaxCount).Append("one-more").ToArray();

            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = mostlyDuplicates };

            Assert.Equal(["same-tag", "one-more"], message.Tags);
        }

        [Fact]
        public void CopyConstructor_PropagatesTagsToTheCopy()
        {
            var source = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Tags = ["urgent"] };

            var copy = new NotificationsChannelFeederMessage(source) { UserId = "user-1" };

            Assert.Equal(["urgent"], copy.Tags);
        }

        [Fact]
        public void CopyConstructor_WithNoTagsSet_PropagatesAnEmptyCollection()
        {
            var source = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };

            var copy = new NotificationsChannelFeederMessage(source) { UserId = "user-1" };

            Assert.Empty(copy.Tags);
        }
    }
}
