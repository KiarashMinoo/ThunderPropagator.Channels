using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #63: EllipsisBody was described as an overflowed form of Body but was never actually
    /// derived from it — every normally constructed message exposed an empty summary regardless of
    /// Body's content. EllipsisBody's getter now derives from Body (truncated to
    /// EllipsisBodyThreshold text elements, "..." appended) whenever it hasn't been explicitly set,
    /// and honors an explicit value — including an empty string — as-is.
    /// </summary>
    public sealed class NotificationsChannelFeederMessageEllipsisBodyTests
    {
        [Fact]
        public void NullBody_ProducesAnEmptySummary()
        {
            var message = new NotificationsChannelFeederMessage();

            Assert.Equal(string.Empty, message.EllipsisBody);
        }

        [Fact]
        public void EmptyBody_ProducesAnEmptySummary()
        {
            var message = new NotificationsChannelFeederMessage { Body = string.Empty };

            Assert.Equal(string.Empty, message.EllipsisBody);
        }

        [Fact]
        public void BodyBelowTheThreshold_IsPreservedWithoutEllipsis()
        {
            var body = new string('a', NotificationsChannelFeederMessage.EllipsisBodyThreshold - 1);
            var message = new NotificationsChannelFeederMessage { Body = body };

            Assert.Equal(body, message.EllipsisBody);
        }

        [Fact]
        public void BodyExactlyAtTheThreshold_IsPreservedWithoutEllipsis()
        {
            var body = new string('a', NotificationsChannelFeederMessage.EllipsisBodyThreshold);
            var message = new NotificationsChannelFeederMessage { Body = body };

            Assert.Equal(body, message.EllipsisBody);
        }

        [Fact]
        public void BodyOneOverTheThreshold_IsTruncatedWithEllipsis()
        {
            var body = new string('a', NotificationsChannelFeederMessage.EllipsisBodyThreshold + 1);
            var message = new NotificationsChannelFeederMessage { Body = body };

            var expected = new string('a', NotificationsChannelFeederMessage.EllipsisBodyThreshold) + "...";
            Assert.Equal(expected, message.EllipsisBody);
        }

        [Fact]
        public void BodyWellOverTheThreshold_IsTruncatedToExactlyTheThresholdPlusEllipsis()
        {
            var body = new string('a', NotificationsChannelFeederMessage.EllipsisBodyThreshold * 3);
            var message = new NotificationsChannelFeederMessage { Body = body };

            Assert.Equal(NotificationsChannelFeederMessage.EllipsisBodyThreshold + 3, message.EllipsisBody.Length);
            Assert.EndsWith("...", message.EllipsisBody);
        }

        [Fact]
        public void ExplicitEllipsisBody_IsHonoredInsteadOfBeingDerived()
        {
            var body = new string('a', NotificationsChannelFeederMessage.EllipsisBodyThreshold + 50);
            var message = new NotificationsChannelFeederMessage { Body = body, EllipsisBody = "custom summary" };

            Assert.Equal("custom summary", message.EllipsisBody);
        }

        [Fact]
        public void ExplicitEmptyEllipsisBody_IsHonoredRatherThanDerived()
        {
            var body = new string('a', NotificationsChannelFeederMessage.EllipsisBodyThreshold + 50);
            var message = new NotificationsChannelFeederMessage { Body = body, EllipsisBody = string.Empty };

            Assert.Equal(string.Empty, message.EllipsisBody);
        }

        [Fact]
        public void UnicodeSurrogatePairs_AreNotSplitAcrossTheBoundary()
        {
            // U+1F600 (😀) is a surrogate pair — two UTF-16 code units. Placing the threshold right
            // in the middle of one proves truncation counts text elements, not raw chars, so it
            // never produces a lone unpaired surrogate.
            var body = new string('a', NotificationsChannelFeederMessage.EllipsisBodyThreshold - 1) + "\U0001F600" + "trailing";
            var message = new NotificationsChannelFeederMessage { Body = body };

            Assert.True(IsWellFormedUtf16(message.EllipsisBody));
            Assert.EndsWith("\U0001F600...", message.EllipsisBody);
        }

        [Fact]
        public void UnicodeCombiningCharacterSequences_AreNotSplitAcrossTheBoundary()
        {
            // "e" + U+0301 (combining acute accent) is a single text element (é) made of two UTF-16
            // chars. Placing it right at the threshold proves truncation treats it as one unit
            // rather than splitting the base character from its combining mark.
            var body = new string('a', NotificationsChannelFeederMessage.EllipsisBodyThreshold - 1) + "é" + "trailing";
            var message = new NotificationsChannelFeederMessage { Body = body };

            Assert.True(IsWellFormedUtf16(message.EllipsisBody));
            Assert.EndsWith("é...", message.EllipsisBody);
        }

        private static bool IsWellFormedUtf16(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsSurrogate(value[i]))
                    continue;

                if (!char.IsHighSurrogate(value[i]) || i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    return false;

                i++;
            }

            return true;
        }
    }
}
