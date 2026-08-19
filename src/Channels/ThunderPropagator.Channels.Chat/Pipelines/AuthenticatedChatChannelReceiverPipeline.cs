using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines
{
    /// <summary>
    /// Issue #109: every Chat pipeline except Login and Register must resolve and validate the
    /// caller's session before touching any pipeline-specific logic — checking authentication ad hoc
    /// in each handler (as several pipelines used to, some only after already mutating state, see
    /// #106) risks omissions and inconsistent responses. Deriving from this base class instead of
    /// AbstractReceivePipeline&lt;ChatChannel&gt; directly is the enforcement mechanism: Invoke runs
    /// the authentication check before InvokeAuthenticatedAsync is called at all, so an
    /// unauthenticated caller can never reach a state mutation. See
    /// ChatChannelPipelineAuthenticationTests for the reflection sweep that fails if a new pipeline
    /// skips this base class.
    /// </summary>
    internal abstract class AuthenticatedChatChannelReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

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
                    if (!chatChannel.TryGetLoggedInUserId(context.WebSocketConnectionInfo.ConnectionId, out var currentUserId))
                        throw new ChatChannelUnauthorizedException();

                    await InvokeAuthenticatedAsync(channelInfo, context, chatChannel, currentUserId, cancellationToken);

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

        /// <summary>
        /// Called only after the caller's session has been resolved. <paramref name="currentUserId"/>
        /// is the validated current-user identifier; implementations must not look it up again via
        /// <c>ChatChannel.LoggedInUsers</c> directly.
        /// </summary>
        protected abstract Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken);
    }
}
