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
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.groups.seticon";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total set-group-icon requests received.");

        public override string RequestKey => $"{nameof(Groups)}/{nameof(SetIcon)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var setGroupIconRequest = context.Request.GetRequestContentFormData<ChatChannelSetGroupIconReceiverPipelineRequestDto>()!;

            var user = await userService.GetByIdAsync(currentUserId, cancellationToken) ?? throw new UserNotFoundException();

            var newGroup = await groupService.SetGroupIconAsync(currentUserId, setGroupIconRequest.GroupId, setGroupIconRequest.Icon, cancellationToken);

            //Send Added Message To User
            chatChannel.EmitMessage(new ChatChannelFeederMessage(
                await messageService.SendMessageAsync(user.Id, user.Id, $"you have changed group icon to {newGroup.GroupIcon}.", cancellationToken)
            ));

            //Send Add Message To Group
            var messages = await messageService.SendMessageToGroupAsync(currentUserId, newGroup.Id, $"User {user.Name} changed group icon to {newGroup.GroupIcon}.", cancellationToken);
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