using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Play
{
    /// <summary>
    /// Wire-facing entry point for <c>Video/Play</c> (see #225) — starts or resumes this channel's one
    /// shared <see cref="VideoPlaybackSession"/> (keyed by <see cref="ChannelInfo.ChannelKey"/>, so every
    /// viewer connecting to this channel instance shares the same session), restricted to whichever
    /// connection is that session's current host — see <see cref="VideoPlaybackSession.IsHost"/>'s own
    /// remarks for #231's deterministic host-ownership design (host status comes only from being the
    /// first eligible subscriber or a subsequent reassignment on disconnect; this pipeline never grants
    /// it). Deliberately does not select a
    /// source itself (that's <c>Video/Select</c>'s job, #228) — a session with no source ever selected is
    /// rejected the same way as one that already <see cref="PlayState.Ended"/> or
    /// <see cref="PlayState.Faulted"/>: none of those are states Play can resume from.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerPlayReceiverPipeline(ILoggerFactory loggerFactory, VideoPlaybackSessionManager sessionManager) : AbstractReceivePipeline<VideoPlayerChannel>(loggerFactory)
    {
        public override string RequestKey => "Video/Play";

        public async Task Invoke(ChannelInfo channelInfo, ReceiveContext context, ReceivePipelineDelegate next, CancellationToken cancellationToken = default)
        {
            if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
            {
                var channel = (VideoPlayerChannel)channelInfo.Channel;
                var session = sessionManager.GetOrCreateSession(channelInfo.ChannelKey.ToString());
                var connectionId = context.WebSocketConnectionInfo.ConnectionId;

                if (!session.IsHost(connectionId))
                    throw new VideoPlayerPlayReceiverPipelineUnauthorizedException();

                if (session.CurrentSource is null)
                    throw new VideoPlayerPlayReceiverPipelineInvalidStateException("Play requires a video already selected via Video/Select.");

                if (session.State is PlayState.Ended or PlayState.Faulted)
                    throw new VideoPlayerPlayReceiverPipelineInvalidStateException($"Play is not valid while this session's state is {session.State}.");

                // Loading/Paused are the only states Play actually needs to act on — Playing/Buffering
                // are already effectively playing (a repeat/concurrent Play here is this AC's own
                // "idempotent" branch, not a no-op error), so calling ResumeAsync would be redundant, not
                // incorrect, but skipping it avoids an unnecessary Pacer.Resume() call and lock
                // acquisition on the hot path a viewer's own Play retry might trigger.
                if (session.State is PlayState.Loading or PlayState.Paused)
                    await session.ResumeAsync(cancellationToken).ConfigureAwait(false);

                var feederMessage = new VideoPlayerChannelFeederMessage
                {
                    SessionId = session.SessionId,
                    State = session.State,
                    Epoch = session.Epoch,
                    // VideoId/Title deliberately left at their empty-string defaults: resolving a
                    // server-side VideoSource to a client-safe playlist id/title is #233's own scope
                    // ("Implement an approved server-side video playlist"), not started yet. Not calling
                    // ValidateForCurrentState() here for the same reason — it would throw on exactly the
                    // data this ticket doesn't have.
                    // CurrentFrameNumber/MediaPosition/SyncTime: VideoPlaybackSession doesn't expose
                    // per-frame telemetry as standalone properties (only via LateJoinSnapshot/individual
                    // packets) — left at their zero defaults rather than adding new session surface area
                    // beyond what #225 actually needs.
                    // Host: no client-identity/display-name system exists in VideoPlayer yet, so the raw
                    // connection id stands in as a placeholder display name until one does.
                    Host = session.HostConnectionId ?? connectionId,
                    ViewerCount = session.ViewerCount
                };

                await channel.BroadcastAsync(feederMessage, cancellationToken).ConfigureAwait(false);

                context.Response.ResponseCode = (int)HttpStatusCode.OK;
                context.Response.ResponseContent = new VideoPlayerPlayReceiverPipelineResponseDto
                {
                    State = session.State,
                    Epoch = session.Epoch
                };
            }
            else
            {
                await next(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
