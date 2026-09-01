using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Messages;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.UserLeave
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelUserLeaveFromGroupReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUserLeaveFromGroupReceiverPipeline(
            ILoggerFactory loggerFactory,
            GroupService groupService,
            UserService userService,
            MessageService messageService)
        : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.groups.userleave";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total leave-group requests received.");

        public override string RequestKey => $"{nameof(Groups)}/{nameof(UserLeave)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var userLeaveRequest = context.Request.GetRequestContentFormData<ChatChannelUserLeaveFromGroupReceiverPipelineRequestDto>()!;

            var user = await userService.GetByIdAsync(currentUserId, cancellationToken) ?? throw new UserNotFoundException();
            var group = await groupService.GetGroupDetailsAsync(currentUserId, userLeaveRequest.GroupId, cancellationToken);

            // Issue #33: sent before the removal below, not after — MessageService.SendMessageToGroupAsync
            // now requires the sender to be a current member of the group, and by the time the user has
            // actually left, they no longer are.
            var messages = await messageService.SendMessageToGroupAsync(currentUserId, group.Id, $"User {user.Name} has left from group.", cancellationToken);
            await Task.WhenAll(messages.Select(message =>
            {
                chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                return Task.CompletedTask;
            }));

            await groupService.RemoveUserFromGroupAsync(currentUserId, group.Id, user.Id, cancellationToken);

            //Send Added Message To User
            chatChannel.EmitMessage(new ChatChannelFeederMessage(
                await messageService.SendMessageAsync(user.Id, user.Id, $"you have left from group {group.Name}.", cancellationToken)
            ));

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = "Left";
        }
    }
}