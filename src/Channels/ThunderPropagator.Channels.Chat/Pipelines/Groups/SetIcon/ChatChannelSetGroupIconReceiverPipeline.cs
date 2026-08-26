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

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.SetIcon
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelSetGroupIconReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelSetGroupIconReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSetGroupIconReceiverPipeline(
            ILoggerFactory loggerFactory,
            GroupService groupService,
            UserService userService,
            MessageService messageService)
        : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Groups)}/{nameof(SetIcon)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var setGroupIconRequest = context.Request.GetRequestContentFormData<ChatChannelSetGroupIconReceiverPipelineRequestDto>()!;

            var user = await userService.GetByIdAsync(currentUserId, cancellationToken) ?? throw new UserNotFoundException();
            var group = await groupService.GetByIdAsync(setGroupIconRequest.GroupId, cancellationToken) ?? throw new GroupNotFoundException();

            var newGroup = await groupService.SetGroupIconAsync(group.Id, setGroupIconRequest.Icon, cancellationToken);

            //Send Added Message To User
            chatChannel.EmitMessage(new ChatChannelFeederMessage(
                await messageService.SendMessageAsync(user.Id, user.Id, $"you have changed group icon to {newGroup.GroupIcon}.", cancellationToken)
            ));

            //Send Add Message To Group
            var messages = await messageService.SendMessageToGroupAsync(currentUserId, group.Id, $"User {user.Name} changed group icon to {newGroup.GroupIcon}.", cancellationToken);
            await Task.WhenAll(messages.Select(message =>
            {
                chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                return Task.CompletedTask;
            }));

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = new ChatChannelSetGroupIconReceiverPipelineResponseDto
            {
                Group = newGroup
            };
        }
    }
}