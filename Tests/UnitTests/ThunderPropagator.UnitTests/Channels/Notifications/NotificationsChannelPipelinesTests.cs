using ThunderPropagator.Channels.Notifications.Pipelines.Acknowledge;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #77: mirrors the reflection-based visibility checks every other receive pipeline in
    /// this codebase has (see e.g. ChatChannelPipelinesTests) — pipeline classes and their DTOs are
    /// internal by convention, not part of the package's public API surface.
    /// </summary>
    public class NotificationsChannelPipelinesTests
    {
        [Fact]
        public void NotificationsAcknowledgeReceiverPipeline_IsInternal()
        {
            var type = typeof(NotificationsAcknowledgeReceiverPipeline<>);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NotificationsAcknowledgeReceiverPipelineRequestDto_IsInternal()
        {
            var type = typeof(NotificationsAcknowledgeReceiverPipelineRequestDto);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NotificationsAcknowledgeReceiverPipelineResponseDto_IsInternal()
        {
            var type = typeof(NotificationsAcknowledgeReceiverPipelineResponseDto);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NotificationsAcknowledgeReceiverPipelineUnauthorizedException_IsException()
        {
            var type = typeof(NotificationsAcknowledgeReceiverPipelineUnauthorizedException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }
    }
}
