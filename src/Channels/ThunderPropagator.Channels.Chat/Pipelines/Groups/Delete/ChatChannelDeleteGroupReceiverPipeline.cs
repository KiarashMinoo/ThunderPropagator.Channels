using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Delete
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelDeleteGroupReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelDeleteGroupReceiverPipeline(ILoggerFactory loggerFactory, GroupService groupService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Groups)}/{nameof(Delete)}";

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
