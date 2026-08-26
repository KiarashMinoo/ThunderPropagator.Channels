using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Join
{
    /// <summary>
    /// Wire-facing entry point for <c>Video/Join</c> (see #230) — attaches the calling connection to this
    /// channel's one shared <see cref="VideoPlaybackSession"/> at its current live position. Unlike every
    /// other <c>Video/*</c> pipeline, this one has no host check (viewing isn't host-restricted) and no
    /// membership pre-check (joining is how a connection becomes a member — checking membership first
    /// would be backwards). Almost everything this ticket's own scope describes — race-safe atomic
    /// registration/snapshot capture, unicasting the latest renderable frame, never creating a new
    /// decoder/timeline, and correct behavior whether the session is playing, paused, mid-seek, or has
    /// ended — is already <see cref="VideoPlaybackSession.Join"/>'s own job (#220/#223); this pipeline's
    /// only added responsibility is marking a reconnect (see <see cref="VideoPlaybackSession.IsSubscribed"/>,
    /// checked before, not after, calling <see cref="VideoPlaybackSession.Join"/> — checking after would
    /// always report <see langword="true"/>, since <c>Join</c> itself subscribes).
    /// <para/>
    /// Deliberately never broadcasts: the issue's own scope says to <b>unicast</b> the latest renderable
    /// frame, not broadcast one — a join doesn't change any shared session state, it only registers one
    /// new viewer, so there is nothing for any other subscriber to be told. This is also how "existing
    /// viewers experience no playback interruption" (this ticket's own AC) is satisfied: the correct way
    /// to guarantee zero interruption is to not touch them at all, which <see cref="VideoPlaybackSession.Join"/>
    /// already doesn't.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerJoinReceiverPipeline(ILoggerFactory loggerFactory, VideoPlaybackSessionManager sessionManager) : AbstractReceivePipeline<VideoPlayerChannel>(loggerFactory)
    {
        public override string RequestKey => "Video/Join";

        public async Task Invoke(ChannelInfo channelInfo, ReceiveContext context, ReceivePipelineDelegate next, CancellationToken cancellationToken = default)
        {
            if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
            {
                var session = sessionManager.GetOrCreateSession(channelInfo.ChannelKey.ToString());
                var connectionId = context.WebSocketConnectionInfo.ConnectionId;

                var wasAlreadySubscribed = session.IsSubscribed(connectionId);
                var snapshot = session.Join(connectionId);

                context.Response.ResponseCode = (int)HttpStatusCode.OK;
                context.Response.ResponseContent = new VideoPlayerJoinReceiverPipelineResponseDto
                {
                    State = snapshot.State,
                    Epoch = snapshot.Epoch,
                    HasBootstrapFrame = snapshot.HasBootstrapFrame,
                    FrameNumber = snapshot.FrameNumber,
                    MediaPosition = (long)snapshot.MediaPosition.TotalMicroseconds,
                    SyncTime = (long)snapshot.SyncTime.TotalMicroseconds,
                    IsReconnect = wasAlreadySubscribed
                };
            }
            else
            {
                await next(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
