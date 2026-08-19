using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Notifications.Pipelines.Acknowledge
{
    /// <summary>
    /// Wire-facing entry point for acknowledging a notification's delivery/read-lifecycle state (see
    /// #77) over an established, subscribed connection. A thin wrapper around
    /// <see cref="NotificationsChannel{T}.AcknowledgeAsync"/> — all the actual merge/idempotency/
    /// concurrency logic lives there, shared with any other caller of that method (e.g. a REST
    /// endpoint or message-broker consumer that authenticates its own callers independently). This
    /// pipeline's only added responsibility is authorization: it resolves the caller's UserId from
    /// <see cref="NotificationsChannel{T}.SubscribedUserIdsByConnectionId"/> — the identity the
    /// connection itself established by subscribing — rather than accepting a UserId supplied in the
    /// request, so a client can never claim to be acknowledging on behalf of a different recipient.
    /// </summary>
    [ReceivePipelineRequestSchema(typeof(NotificationsAcknowledgeReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(NotificationsAcknowledgeReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class NotificationsAcknowledgeReceiverPipeline<TNotificationsChannelConfiguration>(ILoggerFactory loggerFactory)
        : AbstractReceivePipeline<NotificationsChannel<TNotificationsChannelConfiguration>>(loggerFactory)
        where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
    {
        public override string RequestKey => "Acknowledge";

        public async Task Invoke(ChannelInfo channelInfo, ReceiveContext context, ReceivePipelineDelegate next, CancellationToken cancellationToken = default)
        {
            if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
            {
                var request = context.Request.GetRequestContentFormData<NotificationsAcknowledgeReceiverPipelineRequestDto>()!;

                var channel = (NotificationsChannel<TNotificationsChannelConfiguration>)channelInfo.Channel;
                if (!channel.SubscribedUserIdsByConnectionId.TryGetValue(context.WebSocketConnectionInfo.ConnectionId, out var userId))
                    throw new NotificationsAcknowledgeReceiverPipelineUnauthorizedException();

                var state = await channel.AcknowledgeAsync(userId, request.Id, request.State, cancellationToken).ConfigureAwait(false);

                context.Response.ResponseCode = (int)HttpStatusCode.OK;
                context.Response.ResponseContent = new NotificationsAcknowledgeReceiverPipelineResponseDto { State = state };
            }
            else
            {
                await next(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
