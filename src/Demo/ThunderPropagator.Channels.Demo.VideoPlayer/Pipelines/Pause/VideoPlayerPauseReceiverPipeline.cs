using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Pause
{
    /// <summary>
    /// Wire-facing entry point for <c>Video/Pause</c> (see #226) — freezes this channel's one shared
    /// <see cref="VideoPlaybackSession"/> at its current position for every viewer, restricted to
    /// whichever connection is that session's host — see <see cref="VideoPlaybackSession.TryClaimOrVerifyHost"/>'s
    /// own remarks for the temporary minimal ownership model this enforces, pending #231's deterministic
    /// design. Only <see cref="PlayState.Playing"/>/<see cref="PlayState.Buffering"/> actually need a
    /// <see cref="VideoPlaybackSession.PauseAsync"/> call; an already-<see cref="PlayState.Paused"/>
    /// session is this ticket's own "idempotent" AC — re-broadcasting the same retained snapshot rather
    /// than erroring. <see cref="PlayState.Loading"/> is rejected the same way as
    /// <see cref="PlayState.Ended"/>/<see cref="PlayState.Faulted"/>: none of those have an established
    /// current frame for Pause to freeze. Pausing the server audio timeline coherently and stopping
    /// further timed publication both fall out of <see cref="VideoPlaybackSession.PauseAsync"/> itself
    /// (audio shares the same generation's <see cref="Media.FramePacer"/> as video, and both publish loops
    /// already gate on <c>Pacer.IsPaused</c>) — this pipeline adds no separate logic for either.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerPauseReceiverPipeline(ILoggerFactory loggerFactory, VideoPlaybackSessionManager sessionManager) : AbstractReceivePipeline<VideoPlayerChannel>(loggerFactory)
    {
        public override string RequestKey => "Video/Pause";

        public async Task Invoke(ChannelInfo channelInfo, ReceiveContext context, ReceivePipelineDelegate next, CancellationToken cancellationToken = default)
        {
            if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
            {
                var channel = (VideoPlayerChannel)channelInfo.Channel;
                var session = sessionManager.GetOrCreateSession(channelInfo.ChannelKey.ToString());
                var connectionId = context.WebSocketConnectionInfo.ConnectionId;

                if (!session.TryClaimOrVerifyHost(connectionId))
                    throw new VideoPlayerPauseReceiverPipelineUnauthorizedException();

                if (session.CurrentSource is null || session.State is PlayState.Loading or PlayState.Ended or PlayState.Faulted)
                    throw new VideoPlayerPauseReceiverPipelineInvalidStateException($"Pause is not valid while this session's state is {session.State}.");

                if (session.State is PlayState.Playing or PlayState.Buffering)
                    await session.PauseAsync(cancellationToken).ConfigureAwait(false);

                // PeekSnapshot (not a second, separately-timed read of State/Epoch/frame fields) is what
                // keeps "the retained current-frame identity" this AC requires internally consistent —
                // see LateJoinSnapshot's own remarks on why that matters.
                var snapshot = session.PeekSnapshot();

                var feederMessage = new VideoPlayerChannelFeederMessage
                {
                    SessionId = session.SessionId,
                    State = snapshot.State,
                    Epoch = snapshot.Epoch,
                    CurrentFrameNumber = snapshot.FrameNumber,
                    MediaPosition = (long)snapshot.MediaPosition.TotalMicroseconds,
                    SyncTime = (long)snapshot.SyncTime.TotalMicroseconds,
                    // VideoId/Title/Host: same reasoning as #225's VideoPlayerPlayReceiverPipeline — VideoId/
                    // Title need #233's playlist resolution (not started), so they stay at their empty
                    // defaults and ValidateForCurrentState() is deliberately not called; Host stands in as
                    // the raw connection id pending a real client-identity system.
                    Host = session.HostConnectionId ?? connectionId,
                    ViewerCount = session.ViewerCount
                };

                await channel.BroadcastAsync(feederMessage, cancellationToken).ConfigureAwait(false);

                context.Response.ResponseCode = (int)HttpStatusCode.OK;
                context.Response.ResponseContent = new VideoPlayerPauseReceiverPipelineResponseDto
                {
                    State = snapshot.State,
                    Epoch = snapshot.Epoch,
                    FrameNumber = snapshot.FrameNumber,
                    MediaPosition = (long)snapshot.MediaPosition.TotalMicroseconds,
                    SyncTime = (long)snapshot.SyncTime.TotalMicroseconds
                };
            }
            else
            {
                await next(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
