namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    public class ChatChannelPipelinesTests
    {
        [Fact]
        public void ChatChannelLoginReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Login.ChatChannelLoginReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelLoginReceiverPipelineRequestDto_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Login.ChatChannelLoginReceiverPipelineRequestDto);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelLoginReceiverPipelineResponseDto_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Login.ChatChannelLoginReceiverPipelineResponseDto);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelLoginReceiverPipelineInvalidCredentialException_IsException()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Login.ChatChannelLoginReceiverPipelineInvalidCredentialException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void ChatChannelRegisterReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Register.ChatChannelRegisterReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelRegisterReceiverPipelineRequestDto_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Register.ChatChannelRegisterReceiverPipelineRequestDto);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelRegisterReceiverPipelineResponseDto_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Register.ChatChannelRegisterReceiverPipelineResponseDto);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelUpdateUserReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.Update.ChatChannelUpdateUserReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelUserSetNameReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.SetName.ChatChannelUserSetNameReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelUserSetAvatarReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Users.SetAvatar.ChatChannelUserSetAvatarReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelCreateGroupReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.Create.ChatChannelCreateGroupReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelGetGroupsReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.GetAll.ChatChannelGetGroupsReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelJoinUserToGroupReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.Join.ChatChannelJoinUserToGroupReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelRemoveUserToGroupReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.RemoveUser.ChatChannelRemoveUserToGroupReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelUserLeaveFromGroupReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.UserLeave.ChatChannelUserLeaveFromGroupReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelRenameGroupReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.Rename.ChatChannelRenameGroupReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelSetGroupIconReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Pipelines.Groups.SetIcon.ChatChannelSetGroupIconReceiverPipeline);
            Assert.True(type.IsPublic);
        }
    }
}

