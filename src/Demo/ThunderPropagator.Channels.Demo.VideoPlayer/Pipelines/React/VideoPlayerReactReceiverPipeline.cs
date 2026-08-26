using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.React
{
    /// <summary>
    /// Wire-facing entry point for <c>Video/React</c> (see #229) — records a lightweight live reaction
    /// against this channel's one shared <see cref="VideoPlaybackSession"/>, for any currently subscribed
    /// viewer (not just its host — see <see cref="VideoPlaybackSession.IsSubscribed"/>, unlike every
    /// other <c>Video/*</c> pipeline's own <c>TryClaimOrVerifyHost</c> gate). All the validation/rate-
    /// limiting/aggregation/expiry logic this ticket's own scope describes already lives in
    /// <see cref="VideoPlaybackSession.Reactions"/> (a <see cref="ReactionAggregator"/>) — this pipeline's
    /// own job is authorization, mapping a rejection reason to the right HTTP-equivalent status, and
    /// broadcasting the resulting snapshot. See <see cref="ReactionAggregator"/>'s own remarks for why
    /// none of that work can ever delay frame/audio publication.
    /// </summary>
    [ReceivePipelineRequestSchema(typeof(VideoPlayerReactReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(VideoPlayerReactReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerReactReceiverPipeline(ILoggerFactory loggerFactory, VideoPlaybackSessionManager sessionManager) : AbstractReceivePipeline<VideoPlayerChannel>(loggerFactory)
    {
        public override string RequestKey => "Video/React";

        public async Task Invoke(ChannelInfo channelInfo, ReceiveContext context, ReceivePipelineDelegate next, CancellationToken cancellationToken = default)
        {
            if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
            {
                var channel = (VideoPlayerChannel)channelInfo.Channel;
                var session = sessionManager.GetOrCreateSession(channelInfo.ChannelKey.ToString());
                var connectionId = context.WebSocketConnectionInfo.ConnectionId;

                if (!session.IsSubscribed(connectionId))
                    throw new VideoPlayerReactReceiverPipelineUnauthorizedException();

                var request = context.Request.GetRequestContentFormData<VideoPlayerReactReceiverPipelineRequestDto>()!;

                if (!session.Reactions.TryRecord(connectionId, request.Reaction, out var rejectionReason))
                {
                    throw rejectionReason switch
                    {
                        ReactionRejectionReason.RateLimited => new VideoPlayerReactReceiverPipelineRateLimitedException(),
                        _ => new VideoPlayerReactReceiverPipelineInvalidReactionException($"'{request.Reaction}' is not a currently available reaction.")
                    };
                }

                var reactionsSnapshot = session.Reactions.GetSnapshot();
                var sessionSnapshot = session.PeekSnapshot();

                var feederMessage = new VideoPlayerChannelFeederMessage
                {
                    SessionId = session.SessionId,
                    State = sessionSnapshot.State,
                    Epoch = sessionSnapshot.Epoch,
                    CurrentFrameNumber = sessionSnapshot.FrameNumber,
                    MediaPosition = (long)sessionSnapshot.MediaPosition.TotalMicroseconds,
                    SyncTime = (long)sessionSnapshot.SyncTime.TotalMicroseconds,
                    Reactions = reactionsSnapshot,
                    // VideoId/Title/Host: same reasoning as Play/Pause/Seek — this pipeline has no
                    // playlist data (only Video/Select does), so VideoId/Title stay at their empty
                    // defaults and ValidateForCurrentState() is deliberately not called; Host stands in
                    // as the raw connection id pending a real client-identity system.
                    Host = session.HostConnectionId ?? connectionId,
                    ViewerCount = session.ViewerCount
                };

                // "Broadcast compact reaction state/events independently from media packets" (#229's own
                // scope) is exactly what this already is: the control-channel broadcast every other
                // Video/* pipeline already uses, never the VideoFramePacket/AudioFramePacket per-viewer
                // queues, which this was never going to touch anyway.
                await channel.BroadcastAsync(feederMessage, cancellationToken).ConfigureAwait(false);

                context.Response.ResponseCode = (int)HttpStatusCode.OK;
                context.Response.ResponseContent = new VideoPlayerReactReceiverPipelineResponseDto
                {
                    Reactions = reactionsSnapshot
                };
            }
            else
            {
                await next(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
