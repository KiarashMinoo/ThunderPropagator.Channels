using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Get
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelGetUserReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelGetUserReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetUserReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.users.get";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total get-user requests received.");

        public override string RequestKey => $"{nameof(Users)}/{nameof(Get)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var getUserRequest = context.Request.GetRequestContentFormData<ChatChannelGetUserReceiverPipelineRequestDto>()!;

            // Issue #122: every existing user is currently visible to every authenticated caller —
            // this codebase has no blocking/hidden-profile concept at all (see UserService/User).
            // "Unknown or hidden users produce the documented response" therefore only has an
            // "unknown" case to handle today; a future ticket that adds actual visibility rules
            // would extend this same not-found branch rather than needing a new response shape.
            var user = await userService.GetByIdAsync(getUserRequest.UserId, cancellationToken) ?? throw new UserNotFoundException();

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = ChatChannelGetUserReceiverPipelineResponseDto.FromUser(user);
        }
    }
}
