using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Create
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelCreateGroupReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelCreateGroupReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelCreateGroupReceiverPipeline(ILoggerFactory loggerFactory, GroupService groupService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Groups)}/{nameof(Create)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var createGroupRequest = context.Request.GetRequestContentFormData<ChatChannelCreateGroupReceiverPipelineRequestDto>()!;

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = new ChatChannelCreateGroupReceiverPipelineResponseDto
            {
                Group = await groupService.CreateAsync(createGroupRequest.Name, currentUserId, createGroupRequest.Users, cancellationToken)
            };
        }
    }
}