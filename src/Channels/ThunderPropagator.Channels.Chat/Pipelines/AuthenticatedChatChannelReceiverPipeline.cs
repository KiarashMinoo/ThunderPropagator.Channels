using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

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
    ///
    /// Issue #40: the activity/counter name used to be derived at runtime from
    /// $"{channelInfo.ChannelName}_{GetType().Name}_{nameof(Invoke)}" — unstable across renames,
    /// PascalCase (violates OTel's lowercase-dot-separated convention), and one counter per distinct
    /// channel name (unbounded cardinality). Each derived pipeline now supplies its own static,
    /// OTel-convention instrument name and a Counter&lt;long&gt; created once as a static readonly
    /// field — the CLR's type-initializer guarantee makes that inherently race-free, so the
    /// double-checked-locking ChatChannelPipelineTelemetry helper this class used to route through is
    /// gone; the channel is instead carried as the channel.name tag on both the activity and the
    /// counter, so per-pipeline cardinality is bounded by channel count, not by pipeline type name.
    ///
    /// Issue #46: sender identity used to be resolved via ChatChannel.LoggedInUsers, a node-local
    /// dictionary — a request landing on a different cluster node than the one a connection logged
    /// in on would find nothing there. Now resolved via ChatChannel.TryGetLoggedInUserIdAsync, which
    /// queries the persisted, cluster-wide ChatUserSessionService instead — see that method's own
    /// comment for why the lookup lives on ChatChannel rather than being injected into this class
    /// directly.
    /// </summary>
    internal abstract class AuthenticatedChatChannelReceiverPipeline(ILoggerFactory loggerFactory) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        /// <summary>
        /// Static, lowercase dot-separated OTel instrument/span name for this pipeline, e.g.
        /// "thunderpropagator.channels.chat.groups.create".
        /// </summary>
        protected abstract string ActivityName { get; }

        /// <summary>
        /// This pipeline's request counter, named after <see cref="ActivityName"/>.
        /// </summary>
        protected abstract Counter<long>? RequestCounter { get; }

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            using var activity = Telemetry.StartActivity(ActivityName, ActivityKind.Consumer)?
                .SetTag(ChatChannelTelemetryTags.ChannelType, channelInfo.ChannelType)
                .SetTag(ChatChannelTelemetryTags.ChannelKey, channelInfo.ChannelKey)
                .SetTag(ChatChannelTelemetryTags.ChannelName, channelInfo.ChannelName);

            try
            {
                if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
                {
                    var chatChannel = (ChatChannel)channelInfo.Channel;
                    var currentUserId = await chatChannel.TryGetLoggedInUserIdAsync(context.WebSocketConnectionInfo.ConnectionId, cancellationToken);
                    if (currentUserId is null)
                        throw new ChatChannelUnauthorizedException();

                    await InvokeAuthenticatedAsync(channelInfo, context, chatChannel, currentUserId.Value, cancellationToken);

                    RequestCounter?.Add(1, new KeyValuePair<string, object?>(ChatChannelTelemetryTags.ChannelName, channelInfo.ChannelName));
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
        /// <c>ChatChannel.TryGetLoggedInUserIdAsync</c> directly.
        /// </summary>
        protected abstract Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken);
    }
}
