using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

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
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.groups.create";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total create-group requests received.");

        public override string RequestKey => $"{nameof(Groups)}/{nameof(Create)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

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