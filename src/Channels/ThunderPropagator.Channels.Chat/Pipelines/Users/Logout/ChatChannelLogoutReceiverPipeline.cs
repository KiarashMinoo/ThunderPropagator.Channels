using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Logout
{
    // Issue #121: derives directly from AbstractReceivePipeline<ChatChannel> — the same base Login
    // and Register use — rather than AuthenticatedChatChannelReceiverPipeline, and is allow-listed as
    // anonymous in ChatChannelPipelineAuthenticationTests alongside them. That base class throws
    // ChatChannelUnauthorizedException before any pipeline-specific code runs when the connection
    // isn't logged in, which would make a repeated logout throw — directly contradicting this
    // ticket's "repeated logout does not throw or corrupt state". Logout instead checks
    // ChatChannel.TryLogOut's own result and treats "wasn't logged in" as a no-op success, not an
    // authorization failure.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelLogoutReceiverPipeline(ILoggerFactory loggerFactory, UserService userService) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => $"{nameof(Users)}/{nameof(Logout)}";

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            var activityName = $"{channelInfo.ChannelName}_{GetType().GetTypeInfo().Name}_{nameof(Invoke)}";
            _counter ??= Telemetry.CreateCounter<long>($"thunderpropagator.{activityName.ToLowerInvariant().Replace('_', '.')}");

            using var activity = Telemetry.StartActivity(activityName, ActivityKind.Consumer)?
                .SetTag(nameof(ChannelInfo.ChannelType), channelInfo.ChannelType)
                .SetTag(nameof(ChannelInfo.ChannelKey), channelInfo.ChannelKey)
                .SetTag(nameof(ChannelInfo.ChannelName), channelInfo.ChannelName);

            try
            {
                if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
                {
                    var chatChannel = (ChatChannel)channelInfo.Channel;

                    // Only the call that actually removes the session (see TryLogOut's own comment)
                    // publishes offline status — a repeat logout, or one that loses a race against
                    // disconnect cleanup, is a safe no-op past this point.
                    if (chatChannel.TryLogOut(context.WebSocketConnectionInfo.ConnectionId, out var userId))
                    {
                        var contacts = await userService.GetUserContactsAsync(userId, cancellationToken);
                        foreach (var contact in contacts)
                            chatChannel.EmitMessage(new ChatChannelFeederMessage(contact.Id, userId));
                    }

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = "LoggedOut";

                    _counter?.Add(1, new KeyValuePair<string, object?>(nameof(channelInfo.ChannelName), channelInfo.ChannelName));
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
