using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;
using ThunderPropagator.Channels.Demo.VideoPlayer.Playlist;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Select
{
    /// <summary>
    /// Wire-facing entry point for <c>Video/Select</c> (see #228) — switches this channel's one shared
    /// <see cref="VideoPlaybackSession"/> to a different approved video, restricted to whichever
    /// connection is that session's host — see <see cref="VideoPlaybackSession.TryClaimOrVerifyHost"/>'s
    /// own remarks for the temporary minimal ownership model this enforces, pending #231's deterministic
    /// design.
    /// </summary>
    /// <remarks>
    /// Unlike <c>Video/Play</c>/<c>Video/Pause</c>/<c>Video/Seek</c>, this pipeline never rejects based on
    /// <see cref="PlayState"/> — selecting a video is valid from any state, including the very first
    /// selection and switching away from one already playing (<see cref="VideoPlaybackSession.SelectAsync"/>'s
    /// own doc comment says exactly this). The only rejection this pipeline makes beyond authorization is
    /// the requested <c>VideoId</c> not resolving to a currently-selectable <see cref="VideoPlaylistEntry"/>
    /// — see <see cref="VideoPlayerSelectReceiverPipelineVideoNotAvailableException"/>'s own remarks. This
    /// is also the one place in the whole <c>Video/*</c> pipeline family that ever sets
    /// <see cref="VideoPlayerChannelFeederMessage.VideoId"/>/<see cref="VideoPlayerChannelFeederMessage.Title"/>
    /// to a non-empty value: <c>Video/Play</c>/<c>Video/Pause</c>/<c>Video/Seek</c> each leave them at their
    /// empty defaults because none of them has a data source for them — this pipeline does, straight from
    /// its own playlist lookup, so no session-level VideoId/Title tracking is needed for that (deliberately
    /// not added to <see cref="VideoPlaybackSession"/> — see #228's own scope notes).
    /// <para/>
    /// Broadcasts twice, not once: an immediate <see cref="PlayState.Loading"/> broadcast before opening
    /// the source (so clients see a loading indicator promptly — opening real media can take real
    /// wall-clock time), then a final <see cref="PlayState.Playing"/> or <see cref="PlayState.Faulted"/>
    /// broadcast once <see cref="VideoPlaybackSession.SelectAsync"/> resolves — #228's own scope,
    /// "Publish Loading, Playing/Paused, or Faulted state transitions."
    /// </remarks>
    [ReceivePipelineRequestSchema(typeof(VideoPlayerSelectReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(VideoPlayerSelectReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSelectReceiverPipeline(ILoggerFactory loggerFactory, VideoPlaybackSessionManager sessionManager, IVideoPlaylist playlist) : AbstractReceivePipeline<VideoPlayerChannel>(loggerFactory)
    {
        public override string RequestKey => "Video/Select";

        public async Task Invoke(ChannelInfo channelInfo, ReceiveContext context, ReceivePipelineDelegate next, CancellationToken cancellationToken = default)
        {
            if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
            {
                var channel = (VideoPlayerChannel)channelInfo.Channel;
                var session = sessionManager.GetOrCreateSession(channelInfo.ChannelKey.ToString());
                var connectionId = context.WebSocketConnectionInfo.ConnectionId;

                if (!session.TryClaimOrVerifyHost(connectionId))
                    throw new VideoPlayerSelectReceiverPipelineUnauthorizedException();

                var request = context.Request.GetRequestContentFormData<VideoPlayerSelectReceiverPipelineRequestDto>()!;

                // Only ever an entry this server itself registered — VideoId never reaches
                // VideoSource.Location, and an unknown or disabled id is rejected before any source is
                // ever touched, satisfying #228's own AC, "Arbitrary path/URL input cannot reach the
                // decoder."
                if (!playlist.TryGetEntry(request.VideoId, out var entry) || entry is null || !entry.IsEnabled)
                    throw new VideoPlayerSelectReceiverPipelineVideoNotAvailableException();

                // Forced to Loading explicitly rather than read off session.PeekSnapshot(): the session
                // itself hasn't transitioned yet at this point (SelectAsync below hasn't run), so its own
                // State would still report whatever the *previous* generation left it at, not the
                // Loading transition this broadcast exists to announce — #228's own scope, "Publish
                // Loading... state transitions."
                await BroadcastAsync(channel, session, connectionId, entry.VideoId, entry.Title, PlayState.Loading, cancellationToken).ConfigureAwait(false);

                try
                {
                    await session.SelectAsync(entry.Source, TimeSpan.FromMicroseconds(request.StartPositionMicroseconds), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    // session.State is already PlayState.Faulted by the time SwitchGenerationAsync
                    // rethrows — broadcast that, but never the original exception's own message: see
                    // VideoPlayerSelectReceiverPipelineSourceFailedException's own remarks.
                    await BroadcastAsync(channel, session, connectionId, entry.VideoId, entry.Title, PlayState.Faulted, cancellationToken).ConfigureAwait(false);
                    throw new VideoPlayerSelectReceiverPipelineSourceFailedException();
                }

                // SwitchGenerationAsync always transitions straight to Playing on success (same finding
                // as #225/#226/#227) — session.State is already Playing here, this parameter just makes
                // that explicit rather than re-deriving it from the snapshot below.
                await BroadcastAsync(channel, session, connectionId, entry.VideoId, entry.Title, PlayState.Playing, cancellationToken).ConfigureAwait(false);

                context.Response.ResponseCode = (int)HttpStatusCode.OK;
                context.Response.ResponseContent = new VideoPlayerSelectReceiverPipelineResponseDto
                {
                    VideoId = entry.VideoId,
                    Title = entry.Title,
                    State = session.State,
                    Epoch = session.Epoch
                };
            }
            else
            {
                await next(context, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Builds and broadcasts one <see cref="VideoPlayerChannelFeederMessage"/> for one of this
        /// pipeline's three broadcasts (see this class's own remarks). <paramref name="state"/> is always
        /// caller-supplied rather than read off <see cref="VideoPlaybackSession.PeekSnapshot"/>: for the
        /// pre-<c>SelectAsync</c> Loading call the session hasn't transitioned yet, and for the two
        /// post-<c>SelectAsync</c> calls the caller already knows the outcome (success/failure) more
        /// directly than re-deriving it from a snapshot taken microseconds later would.
        /// </summary>
        private static async Task BroadcastAsync(VideoPlayerChannel channel, VideoPlaybackSession session, string connectionId, string videoId, string title, PlayState state, CancellationToken cancellationToken)
        {
            var snapshot = session.PeekSnapshot();

            var feederMessage = new VideoPlayerChannelFeederMessage
            {
                SessionId = session.SessionId,
                VideoId = videoId,
                Title = title,
                State = state,
                Epoch = snapshot.Epoch,
                CurrentFrameNumber = snapshot.FrameNumber,
                MediaPosition = (long)snapshot.MediaPosition.TotalMicroseconds,
                SyncTime = (long)snapshot.SyncTime.TotalMicroseconds,
                // Host: no client-identity/display-name system exists yet, same reasoning as #225/#226/#227.
                Host = session.HostConnectionId ?? connectionId,
                ViewerCount = session.ViewerCount
                // SourceFrameRate deliberately left at its zero default: this pipeline has no frame-rate
                // value available (VideoPlaybackSession doesn't expose one), which is also why
                // ValidateForCurrentState() is never called here — it would throw on exactly this field
                // while State is Playing/Paused/Buffering.
            };

            await channel.BroadcastAsync(feederMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}
