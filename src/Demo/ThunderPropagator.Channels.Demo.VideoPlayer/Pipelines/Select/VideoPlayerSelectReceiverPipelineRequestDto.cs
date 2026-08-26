using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Select
{
    /// <summary>Wire request for <see cref="VideoPlayerSelectReceiverPipeline"/> (see #228) — the approved video to select.</summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSelectReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        /// <summary>The client-safe playlist id to select — never a path or URL; resolved against <see cref="Playlist.IVideoPlaylist"/>.</summary>
        public required string VideoId
        {
            get => (string)this[nameof(VideoId)];
            set => this[nameof(VideoId)] = value;
        }

        /// <summary>Where to start playback, in microseconds — optional, defaults to the beginning. Same convention as <see cref="Messages.VideoPlayerChannelFeederMessage.MediaPosition"/>.</summary>
        public long StartPositionMicroseconds
        {
            get => (long)GetValueOrDefault(nameof(StartPositionMicroseconds), 0L)!;
            set => this[nameof(StartPositionMicroseconds)] = value;
        }
    }
}
