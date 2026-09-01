using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.SetName
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelUserSetNameReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUserSetNameReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.users.setname";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total set-name requests received.");

        public override string RequestKey => $"{nameof(Users)}/{nameof(SetName)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

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