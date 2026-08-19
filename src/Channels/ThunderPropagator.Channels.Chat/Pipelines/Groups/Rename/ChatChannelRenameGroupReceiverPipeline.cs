using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Rename
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelRenameGroupReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelRenameGroupReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelRenameGroupReceiverPipeline(
            ILoggerFactory loggerFactory,
            GroupService groupService,
            UserService userService,
            MessageService messageService)
        : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Groups)}/{nameof(Rename)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var renameGroupRequest = context.Request.GetRequestContentFormData<ChatChannelRenameGroupReceiverPipelineRequestDto>()!;

            var user = await userService.GetByIdAsync(currentUserId, cancellationToken) ?? throw new UserNotFoundException();
            var group = await groupService.GetByIdAsync(renameGroupRequest.GroupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.Name == renameGroupRequest.Name)
                throw new InvalidOperationException("New group name must be different from existing group name");

            var oldGroupName = group.Name;
            var newGroup = await groupService.RenameGroupAsync(group.Id, renameGroupRequest.Name, cancellationToken);

            //Send Added Message To User
            chatChannel.EmitMessage(new ChatChannelFeederMessage(
                await messageService.SendMessageAsync(user.Id, user.Id, $"you have renamed group from {oldGroupName} to {newGroup.Name}.", cancellationToken)
            ));

            //Send Add Message To Group
            var messages = await messageService.SendMessageToGroupAsync(currentUserId, group.Id, $"User {user.Name} has renamed group from {oldGroupName} to {newGroup.Name}.", cancellationToken);
            await Task.WhenAll(messages.Select(message =>
            {
                chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                return Task.CompletedTask;
            }));

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = new ChatChannelRenameGroupReceiverPipelineResponseDto
            {
                Group = newGroup
            };
        }
    }
}