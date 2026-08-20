namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    public class ChatChannelPipelinesTests
    {
        [Fact]
        public void ChatChannelLoginReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Login.ChatChannelLoginReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelLoginReceiverPipelineRequestDto_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Login.ChatChannelLoginReceiverPipelineRequestDto);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelLoginReceiverPipelineResponseDto_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Login.ChatChannelLoginReceiverPipelineResponseDto);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelLoginReceiverPipelineInvalidCredentialException_IsException()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Login.ChatChannelLoginReceiverPipelineInvalidCredentialException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void ChatChannelRegisterReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Register.ChatChannelRegisterReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelRegisterReceiverPipelineRequestDto_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Register.ChatChannelRegisterReceiverPipelineRequestDto);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelRegisterReceiverPipelineResponseDto_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Register.ChatChannelRegisterReceiverPipelineResponseDto);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelUpdateUserReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Update.ChatChannelUpdateUserReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelUserSetNameReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.SetName.ChatChannelUserSetNameReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelUserSetAvatarReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.SetAvatar.ChatChannelUserSetAvatarReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelCreateGroupReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.Create.ChatChannelCreateGroupReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelGetGroupsReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.GetAll.ChatChannelGetGroupsReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelJoinUserToGroupReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.Join.ChatChannelJoinUserToGroupReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelRemoveUserToGroupReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.RemoveUser.ChatChannelRemoveUserToGroupReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelUserLeaveFromGroupReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.UserLeave.ChatChannelUserLeaveFromGroupReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelRenameGroupReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.Rename.ChatChannelRenameGroupReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelSetGroupIconReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.SetIcon.ChatChannelSetGroupIconReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelGetMessageHistoryReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Messages.History.ChatChannelGetMessageHistoryReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelGetMessageHistoryReceiverPipelineRequestDto_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Messages.History.ChatChannelGetMessageHistoryReceiverPipelineRequestDto);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelGetMessageHistoryReceiverPipelineResponseDto_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Messages.History.ChatChannelGetMessageHistoryReceiverPipelineResponseDto);
            Assert.True(type.IsNotPublic);
        }
    }
}

