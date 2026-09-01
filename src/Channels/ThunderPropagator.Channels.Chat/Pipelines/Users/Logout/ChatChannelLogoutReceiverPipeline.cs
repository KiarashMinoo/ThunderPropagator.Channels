using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Sessions;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Messages;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Logout
{
    // Issue #121: derives directly from AbstractReceivePipeline<ChatChannel> — the same base Login
    // and Register use — rather than AuthenticatedChatChannelReceiverPipeline, and is allow-listed as
    // anonymous in ChatChannelPipelineAuthenticationTests alongside them. That base class throws
    // ChatChannelUnauthorizedException before any pipeline-specific code runs when the connection
    // isn't logged in, which would make a repeated logout throw — directly contradicting this
    // ticket's "repeated logout does not throw or corrupt state". Logout instead checks
    // ChatUserSessionService.LogOutAsync's own result and treats "wasn't logged in" as a no-op
    // success, not an authorization failure.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelLogoutReceiverPipeline(ILoggerFactory loggerFactory, UserService userService, ChatUserSessionService sessionService) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.users.logout";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total logout requests received.");

        public override string RequestKey => $"{nameof(Users)}/{nameof(Logout)}";

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            using var activity = Telemetry.StartActivity(TelemetryActivityName, ActivityKind.Consumer)?
                .SetTag(ChatChannelTelemetryTags.ChannelType, channelInfo.ChannelType)
                .SetTag(ChatChannelTelemetryTags.ChannelKey, channelInfo.ChannelKey)
                .SetTag(ChatChannelTelemetryTags.ChannelName, channelInfo.ChannelName);

            try
            {
                if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
                {
                    var chatChannel = (ChatChannel)channelInfo.Channel;

                    // Only the call that actually removes the persisted session (see
                    // ChatUserSessionService.LogOutAsync's own comment) publishes offline status — a
                    // repeat logout, or one that loses a race against disconnect cleanup, is a safe
                    // no-op past this point.
                    var userId = await sessionService.LogOutAsync(context.WebSocketConnectionInfo.ConnectionId, cancellationToken);
                    if (userId is not null)
                    {
                        var contacts = await userService.GetUserContactsAsync(userId.Value, cancellationToken);
                        foreach (var contact in contacts)
                            chatChannel.EmitMessage(new ChatChannelFeederMessage(contact.Id, userId.Value));
                    }

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = "LoggedOut";

                    TelemetryRequestCounter?.Add(1, new KeyValuePair<string, object?>(ChatChannelTelemetryTags.ChannelName, channelInfo.ChannelName));
                }
                else
                {
                    await next(context, cancellationToken);
                }
            }
            finally
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
        }
    }
}
