using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Messages;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Join
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelJoinUserToGroupReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelJoinUserToGroupReceiverPipeline(
            ILoggerFactory loggerFactory,
            GroupService groupService,
            UserService userService,
            MessageService messageService)
        : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Groups)}/{nameof(Join)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var joinUserRequest = context.Request.GetRequestContentFormData<ChatChannelJoinUserToGroupReceiverPipelineRequestDto>()!;

            var user = await userService.GetByIdAsync(currentUserId, cancellationToken) ?? throw new UserNotFoundException();
            var group = await groupService.GetByIdAsync(joinUserRequest.GroupId, cancellationToken) ?? throw new GroupNotFoundException();

            await groupService.AddUserToGroupAsync(group.Id, user.Id, cancellationToken);

            //Send Added Message To User
            chatChannel.EmitMessage(new ChatChannelFeederMessage(
                await messageService.SendMessageAsync(user.Id, user.Id, $"you have joined to group {group.Name}.", cancellationToken)
            ));

            //Send Add Message To Group
            var messages = await messageService.SendMessageToGroupAsync(currentUserId, group.Id, $"User {user.Name} has joined to group.", cancellationToken);
            await Task.WhenAll(messages.Select(message =>
            {
                chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                return Task.CompletedTask;
            }));

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = "Joined";
        }
    }
}