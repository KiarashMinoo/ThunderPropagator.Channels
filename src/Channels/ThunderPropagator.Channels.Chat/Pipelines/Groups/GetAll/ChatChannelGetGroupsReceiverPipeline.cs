using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Endpoints;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.GetAll
{
    [ReceivePipelineResponseSchema(typeof(ChatChannelGetGroupsReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetGroupsReceiverPipeline(ILoggerFactory loggerFactory, GroupService groupService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.groups.getall";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total get-groups requests received.");

        public override string RequestKey => $"{nameof(Groups)}/{nameof(GetAll)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var groups = await groupService.GetAllAsync(cancellationToken);

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = new ChatChannelGetGroupsReceiverPipelineResponseDto
            {
                Groups = groups.Select(ChatChannelGroupSummaryDto.FromGroup).ToList()
            };
        }
    }
}