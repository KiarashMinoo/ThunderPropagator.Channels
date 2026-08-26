using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Seek
{
    /// <summary>
    /// Wire-facing entry point for <c>Video/Seek</c> (see #227) — re-seeks this channel's one shared
    /// <see cref="VideoPlaybackSession"/> to a new position for every viewer, restricted to whichever
    /// connection is that session's current host — see <see cref="VideoPlaybackSession.IsHost"/>'s own
    /// remarks for #231's deterministic host-ownership design.
    /// </summary>
    /// <remarks>
    /// Almost everything this issue's own Scope section describes — cancelling old-epoch decode/pacing
    /// work and awaiting its bounded shutdown, incrementing the epoch exactly once, flushing the last
    /// published packet so no stale frame survives, reopening the source at the new position, and
    /// guaranteeing no old-epoch packet can be published after the seek commits — is already
    /// <see cref="VideoPlaybackSession.SeekAsync"/>'s own job (it shares <see cref="VideoPlaybackSession"/>'s
    /// private generation-switch machinery with <see cref="VideoPlaybackSession.SelectAsync"/>, guarded by
    /// that type's own lifecycle lock, which is also what already serializes concurrent seeks against each
    /// other with a natural last-committed-wins outcome — this pipeline adds no locking of its own). This
    /// pipeline's own job is authorization, clamping the requested position, and broadcasting the result.
    /// <para/>
    /// Unlike <c>Video/Play</c>/<c>Video/Pause</c>, a seek is valid from every <see cref="PlayState"/> once
    /// a source has ever been selected — see <see cref="VideoPlayerSeekReceiverPipelineInvalidStateException"/>'s
    /// own remarks for why re-seeking after <see cref="PlayState.Ended"/>/<see cref="PlayState.Faulted"/>
    /// is deliberately allowed here.
    /// </remarks>
    [ReceivePipelineRequestSchema(typeof(VideoPlayerSeekReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(VideoPlayerSeekReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSeekReceiverPipeline(ILoggerFactory loggerFactory, VideoPlaybackSessionManager sessionManager) : AbstractReceivePipeline<VideoPlayerChannel>(loggerFactory)
    {
        public override string RequestKey => "Video/Seek";

        public async Task Invoke(ChannelInfo channelInfo, ReceiveContext context, ReceivePipelineDelegate next, CancellationToken cancellationToken = default)
        {
            if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
            {
                var channel = (VideoPlayerChannel)channelInfo.Channel;
                var session = sessionManager.GetOrCreateSession(channelInfo.ChannelKey.ToString());
                var connectionId = context.WebSocketConnectionInfo.ConnectionId;

                if (!session.IsHost(connectionId))
                    throw new VideoPlayerSeekReceiverPipelineUnauthorizedException();

                if (session.CurrentSource is null)
                    throw new VideoPlayerSeekReceiverPipelineInvalidStateException("Seek requires a video already selected via Video/Select.");

                var request = context.Request.GetRequestContentFormData<VideoPlayerSeekReceiverPipelineRequestDto>()!;
                var requestedPosition = TimeSpan.FromMicroseconds(request.PositionMicroseconds);

                // Zero Duration means "unknown/live" (see VideoPlaybackSession.Duration's own remarks),
                // not "the video is zero-length" — only clamp against a known, positive upper bound.
                var clampedPosition = requestedPosition < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : session.Duration is { } duration && duration > TimeSpan.Zero && requestedPosition > duration
                        ? duration
                        : requestedPosition;

                await session.SeekAsync(clampedPosition, cancellationToken).ConfigureAwait(false);

                // PeekSnapshot immediately after a committed seek will very likely report
                // HasBootstrapFrame == false with every frame field at zero — SwitchGenerationAsync starts
                // the new generation's decode/publish loops asynchronously and returns once the new
                // generation exists, without waiting for its first frame to actually publish. That is
                // expected and correct: what this issue's AC actually requires broadcasting synchronously
                // is the new Epoch (already incremented by the time SeekAsync's Task completes) and State
                // — the first real frame identity reaches viewers through the normal packet-publish path
                // once decode catches up, not through this control-channel broadcast. Deliberately not
                // waiting/polling for a first frame here, which would contradict "await bounded shutdown."
                var snapshot = session.PeekSnapshot();

                var feederMessage = new VideoPlayerChannelFeederMessage
                {
                    SessionId = session.SessionId,
                    State = snapshot.State,
                    Epoch = snapshot.Epoch,
                    CurrentFrameNumber = snapshot.FrameNumber,
                    MediaPosition = (long)snapshot.MediaPosition.TotalMicroseconds,
                    SyncTime = (long)snapshot.SyncTime.TotalMicroseconds,
                    // VideoId/Title/Host: same reasoning as #225/#226 — VideoId/Title need #233's playlist
                    // resolution (not started), so they stay at their empty defaults and
                    // ValidateForCurrentState() is deliberately not called; Host stands in as the raw
                    // connection id pending a real client-identity system.
                    Host = session.HostConnectionId ?? connectionId,
                    ViewerCount = session.ViewerCount
                };

                await channel.BroadcastAsync(feederMessage, cancellationToken).ConfigureAwait(false);

                context.Response.ResponseCode = (int)HttpStatusCode.OK;
                context.Response.ResponseContent = new VideoPlayerSeekReceiverPipelineResponseDto
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
