using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Metadata
{
    // Issue #215: describes VideoPlayerChannelFeederMessage's fields, mirroring QuizChannelMetadata's
    // own shape (#185) — SessionId is the sole subscribing key, State uses the enum descriptor,
    // Epoch/CurrentFrameNumber/MediaPosition/SyncTime/ViewerCount/Duration use the numeric descriptor
    // (DataType.Number is a C# long on the wire — see NumberChannelProgramsDescriptor), SourceFrameRate
    // uses the fractional Decimal descriptor since it is not a whole number, and Reactions is
    // JSON-encoded since it is a collection, not a scalar.
    public
#if !DEBUG
        sealed
#endif
        class VideoPlayerChannelMetadata : AbstractChannelMetadata<VideoPlayerChannel>
    {
        public const string VideoPlayerSession = nameof(VideoPlayerSession);

        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new()
        {
            new SubscribingKeyChannelProgramsDescriptor(0, nameof(VideoPlayerChannelFeederMessage.SessionId), "The playback session identifier").SetTable(VideoPlayerSession),
            new ChannelProgramsDescriptor(1, nameof(VideoPlayerChannelFeederMessage.VideoId), DataType.String, "The current video's client-safe identifier — never its underlying source path/URL").SetTable(VideoPlayerSession),
            new ChannelProgramsDescriptor(2, nameof(VideoPlayerChannelFeederMessage.Title), DataType.String, "The current video's human-readable title").SetTable(VideoPlayerSession),
            new EnumChannelProgramsDescriptor<PlayState>(3, nameof(VideoPlayerChannelFeederMessage.State), "The session's current lifecycle state").SetTable(VideoPlayerSession),
            new NumberChannelProgramsDescriptor(4, nameof(VideoPlayerChannelFeederMessage.Epoch), "The session's current stream epoch").SetTable(VideoPlayerSession),
            new NumberChannelProgramsDescriptor(5, nameof(VideoPlayerChannelFeederMessage.CurrentFrameNumber), "0-based number of the most recently published frame").SetTable(VideoPlayerSession),
            new NumberChannelProgramsDescriptor(6, nameof(VideoPlayerChannelFeederMessage.MediaPosition), "Playback position, in microseconds, as of SyncTime").SetTable(VideoPlayerSession),
            new NumberChannelProgramsDescriptor(7, nameof(VideoPlayerChannelFeederMessage.SyncTime), "Server media clock elapsed-microseconds reading MediaPosition was measured at").SetTable(VideoPlayerSession),
            new ChannelProgramsDescriptor(8, nameof(VideoPlayerChannelFeederMessage.Host), DataType.String, "The current host's display name").SetTable(VideoPlayerSession),
            new NumberChannelProgramsDescriptor(9, nameof(VideoPlayerChannelFeederMessage.ViewerCount), "Number of connections currently subscribed to this session").SetTable(VideoPlayerSession),
            new NumberChannelProgramsDescriptor(10, nameof(VideoPlayerChannelFeederMessage.Duration), "The current video's total duration, in microseconds — zero if unknown/indeterminate").SetTable(VideoPlayerSession),
            new ChannelProgramsDescriptor(11, nameof(VideoPlayerChannelFeederMessage.SourceFrameRate), DataType.Decimal, "The underlying source's own frame rate, in frames per second").SetTable(VideoPlayerSession),
            new JsonChannelProgramsDescriptor(12, nameof(VideoPlayerChannelFeederMessage.Reactions), "Current aggregate reaction counts").SetTable(VideoPlayerSession)
        };
    }
}
