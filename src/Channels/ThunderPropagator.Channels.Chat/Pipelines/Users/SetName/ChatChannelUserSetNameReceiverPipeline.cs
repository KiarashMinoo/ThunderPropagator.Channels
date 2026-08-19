using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.SetName
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelUserSetNameReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUserSetNameReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Users)}/{nameof(SetName)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var setNameRequest = context.Request.GetRequestContentFormData<ChatChannelUserSetNameReceiverPipelineRequestDto>()!;

            await userService.SetNameAsync(currentUserId, setNameRequest.Name, cancellationToken);

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = "Set";
        }
    }
}