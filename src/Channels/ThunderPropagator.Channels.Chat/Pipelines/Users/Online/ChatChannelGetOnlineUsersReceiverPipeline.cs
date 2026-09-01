using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Online
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelGetOnlineUsersReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelGetOnlineUsersReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetOnlineUsersReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.users.online";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total get-online-users requests received.");

        public override string RequestKey => $"{nameof(Users)}/{nameof(Online)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var onlineRequest = context.Request.GetRequestContentFormData<ChatChannelGetOnlineUsersReceiverPipelineRequestDto>()!;

            // Distinct: a user with more than one open connection has more than one entry in
            // LoggedInUsers (one per connectionId), but appears exactly once in the online list —
            // "online" is a property of the user, not of any single connection.
            var onlineUserIds = chatChannel.LoggedInUsers.Values.Distinct().ToList();

            var page = await userService.GetOnlineContactsAsync(currentUserId, onlineUserIds, onlineRequest.Page, onlineRequest.PageSize, cancellationToken);

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = new ChatChannelGetOnlineUsersReceiverPipelineResponseDto
            {
                Users = page.Users.Select(ChatChannelGetUserReceiverPipelineResponseDto.FromUser).ToList(),
                TotalCount = page.TotalCount,
                Page = page.Page,
                PageSize = page.PageSize
            };
        }
    }
}
