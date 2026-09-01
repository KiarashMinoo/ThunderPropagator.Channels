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

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Search
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelSearchUsersReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelSearchUsersReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSearchUsersReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.users.search";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total search-users requests received.");

        public override string RequestKey => $"{nameof(Users)}/{nameof(Search)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var searchRequest = context.Request.GetRequestContentFormData<ChatChannelSearchUsersReceiverPipelineRequestDto>()!;

            var page = await userService.SearchUsersAsync(searchRequest.Term, searchRequest.Page, searchRequest.PageSize, cancellationToken);

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = new ChatChannelSearchUsersReceiverPipelineResponseDto
            {
                Users = page.Users.Select(ChatChannelGetUserReceiverPipelineResponseDto.FromUser).ToList(),
                TotalCount = page.TotalCount,
                Page = page.Page,
                PageSize = page.PageSize
            };
        }
    }
}
