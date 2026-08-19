using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.AddUser
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelAddUserToGroupReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelAddUserToGroupReceiverPipeline(
            ILoggerFactory loggerFactory,
            GroupService groupService,
            UserService userService,
            MessageService messageService)
        : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Groups)}/{nameof(AddUser)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var addUserRequest = context.Request.GetRequestContentFormData<ChatChannelAddUserToGroupReceiverPipelineRequestDto>()!;

            var user = await userService.GetByIdAsync(addUserRequest.UserId, cancellationToken) ?? throw new UserNotFoundException();
            var group = await groupService.GetByIdAsync(addUserRequest.GroupId, cancellationToken) ?? throw new GroupNotFoundException();

            await groupService.AddUserToGroupAsync(group.Id, user.Id, cancellationToken);

            var sender = (await userService.GetByIdAsync(currentUserId, cancellationToken))!;

            //Send Added Message To User
            chatChannel.EmitMessage(new ChatChannelFeederMessage(
                await messageService.SendMessageAsync(currentUserId, user.Id, $"{sender.Name} has added you to group {group.Name}.", cancellationToken)
            ));

            //Send Add Message To Group
            var messages = await messageService.SendMessageToGroupAsync(currentUserId, group.Id, $"User {sender.Name} has added user {user.Name} to group.", cancellationToken);
            await Task.WhenAll(messages.Select(message =>
            {
                chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                return Task.CompletedTask;
            }));

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = "Added";
        }
    }
}