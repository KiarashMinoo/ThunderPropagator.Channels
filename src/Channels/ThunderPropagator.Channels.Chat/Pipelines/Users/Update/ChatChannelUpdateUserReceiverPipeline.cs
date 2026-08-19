using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Update
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelUpdateUserReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelUpdateUserReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUpdateUserReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Users)}/{nameof(Update)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var updateRequest = context.Request.GetRequestContentFormData<ChatChannelUpdateUserReceiverPipelineRequestDto>()!;

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = new ChatChannelUpdateUserReceiverPipelineResponseDto
            {
                User = await userService.UpdateAsync(currentUserId, updateRequest.Bio, updateRequest.BirthDate, cancellationToken)
            };
        }
    }
}