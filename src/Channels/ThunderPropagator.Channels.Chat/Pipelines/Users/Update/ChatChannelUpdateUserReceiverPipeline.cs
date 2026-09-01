using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

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
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.users.update";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total update-user requests received.");

        public override string RequestKey => $"{nameof(Users)}/{nameof(Update)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

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