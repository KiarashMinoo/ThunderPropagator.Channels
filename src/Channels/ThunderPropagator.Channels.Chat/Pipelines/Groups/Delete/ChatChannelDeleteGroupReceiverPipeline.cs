using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Messages;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Delete
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelDeleteGroupReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelDeleteGroupReceiverPipeline(ILoggerFactory loggerFactory, GroupService groupService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.groups.delete";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total delete-group requests received.");

        public override string RequestKey => $"{nameof(Groups)}/{nameof(Delete)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var deleteRequest = context.Request.GetRequestContentFormData<ChatChannelDeleteGroupReceiverPipelineRequestDto>()!;

            var (group, affectedMemberIds) = await groupService.DeleteGroupAsync(currentUserId, deleteRequest.GroupId, cancellationToken);

            foreach (var memberId in affectedMemberIds)
                chatChannel.EmitMessage(new ChatChannelFeederMessage(memberId, group.Id, currentUserId));

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = "Deleted";
        }
    }
}
