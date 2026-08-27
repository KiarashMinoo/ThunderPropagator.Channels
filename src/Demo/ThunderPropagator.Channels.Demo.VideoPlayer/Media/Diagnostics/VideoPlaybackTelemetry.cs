using System.Diagnostics;
using System.Diagnostics.Metrics;
using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Diagnostics
{
    /// <summary>
    /// Aggregates <see cref="Media.Session.VideoPlaybackSession"/>'s per-frame telemetry into real
    /// counters/histograms and sampled per-frame diagnostics — #235's own scope in full. Every instrument
    /// is process-wide (created once, shared by every session that is handed an instance of this class),
    /// mirroring how <see cref="Telemetry"/>'s own <c>Meter</c>/<c>ActivitySource</c> are process-wide;
    /// only the optional per-frame <see cref="StartSampledFrameActivity"/> sampling counters are
    /// per-instance.
    /// </summary>
    /// <remarks>
    /// <b>Why metric tags stay bounded:</b> <see cref="VideoPlaybackMediaType"/> (2 values),
    /// <see cref="FrameDropReason"/> (3 values), and the small fixed set of failure-stage strings this
    /// type itself passes to <see cref="RecordSessionFailure"/> are the only tags any instrument here
    /// ever carries — never a session id, viewer id, or frame number, which are unbounded and would blow
    /// up a metrics backend's own cardinality — #235's own AC, "Tag by bounded identifiers; avoid
    /// high-cardinality viewer/frame tags." Per-frame detail (session id, epoch, frame number, media
    /// position) instead rides on <see cref="StartSampledFrameActivity"/>'s sampled <see cref="Activity"/>,
    /// which is the correct place for unbounded, per-instance identifiers: a trace backend stores spans
    /// per-instance by design, unlike a metrics backend aggregating by tag combination.
    /// <para/>
    /// <b>Why no source URL/path ever reaches an instrument or a sampled activity here:</b> nothing in
    /// this type ever accepts a <see cref="Media.VideoSource"/> or its own <c>Location</c> — every method
    /// here takes only already-safe, bounded values (enums, counts, durations, byte lengths, the caller's
    /// own session/epoch/frame identifiers) — #235's own AC, "Metrics do not leak source URLs/paths or
    /// credentials."
    /// </remarks>
    public sealed class VideoPlaybackTelemetry
    {
        private const string MediaTypeTagName = "media.type";
        private const string DropReasonTagName = "drop.reason";
        private const string FailureStageTagName = "failure.stage";

        // Every instrument is nullable: Telemetry.Create* itself returns null when Telemetry.Configure has
        // never been called (e.g. a unit test that never bootstraps telemetry) — every Record* method
        // below null-conditionally no-ops in that case rather than throwing.
        private static readonly Counter<long>? FramesDecodedCounter = Telemetry.CreateCounter<long>(
            "thunderpropagator.videoplayer.media.frames.decoded", "{frame}",
            "Frames (video) or decoded audio frames (audio) successfully pulled from the source, tagged by media.type.");

        private static readonly Counter<long>? FramesEncodedCounter = Telemetry.CreateCounter<long>(
            "thunderpropagator.videoplayer.media.frames.encoded", "{frame}",
            "Frames successfully encoded for the wire, tagged by media.type. For audio, one decoded frame can yield zero, one, or more encoded chunks — see AudioFrameEncoder's own remarks — so this counts encoder invocations, not chunks.");

        private static readonly Counter<long>? FramesPublishedCounter = Telemetry.CreateCounter<long>(
            "thunderpropagator.videoplayer.media.frames.published", "{frame}",
            "Frames actually delivered to at least the session's own last-known-frame slot (fan-out to zero or more subscribers), tagged by media.type.");

        private static readonly Counter<long>? FramesAcknowledgedCounter = Telemetry.CreateCounter<long>(
            "thunderpropagator.videoplayer.media.frames.acknowledged", "{frame}",
            "Frames a client has confirmed it rendered, tagged by media.type. No receive pipeline reports client-side rendering yet, so nothing calls RecordFrameAcknowledged today — a future client-ack pipeline is the intended caller, per #235's own AC, \"decoded, encoded, published, acknowledged/rendered where available.\"");

        private static readonly Counter<long>? FramesDroppedCounter = Telemetry.CreateCounter<long>(
            "thunderpropagator.videoplayer.media.frames.dropped", "{frame}",
            "Frames dropped before publication, tagged by media.type and drop.reason (FrameDropReason) — #235's own AC, \"Slow-subscriber and late-frame drops have distinguishable reasons.\"");

        private static readonly Counter<long>? BytesPublishedCounter = Telemetry.CreateCounter<long>(
            "thunderpropagator.videoplayer.media.bytes", "By",
            "Encoded payload bytes published, tagged by media.type. A monotonic counter, not a pre-computed rate — apply rate() in the metrics backend for bytes/sec, the standard OpenTelemetry pattern this AC's own \"bytes/sec\" signal maps to.");

        private static readonly Counter<long>? SessionFailuresCounter = Telemetry.CreateCounter<long>(
            "thunderpropagator.videoplayer.media.session.failures", "{failure}",
            "Unrecoverable session failures, tagged by failure.stage (\"open\" — SelectAsync/SeekAsync failed to open a source — or \"playback\" — an active generation's decode/publish loop faulted).");

        private static readonly Histogram<double>? DecodeDurationHistogram = Telemetry.CreateHistogram<double>(
            "thunderpropagator.videoplayer.media.decode.duration", "ms",
            "Wall-clock time to produce one decoded frame from the source, tagged by media.type.");

        private static readonly Histogram<double>? EncodeDurationHistogram = Telemetry.CreateHistogram<double>(
            "thunderpropagator.videoplayer.media.encode.duration", "ms",
            "Wall-clock time spent inside the encoder for one decoded frame (video: the encodeFrame delegate; audio: one Encode/Flush call), tagged by media.type.");

        private static readonly Histogram<double>? PublishLatencyHistogram = Telemetry.CreateHistogram<double>(
            "thunderpropagator.videoplayer.media.publish.latency", "ms",
            "Wall-clock time spent building and fanning a frame out to every currently subscribed viewer (packet construction, encode included, plus the publish-gate critical section), tagged by media.type — grows with subscriber count, unlike decode/encode duration.");

        private static readonly Histogram<double>? PacingDriftHistogram = Telemetry.CreateHistogram<double>(
            "thunderpropagator.videoplayer.media.pacing.drift", "ms",
            "How far the actual publish moment is from FramePacer's own computed schedule for that frame (FramePacer.GetPacingError) — positive means late, tagged by media.type. Diagnostic only, exactly as FramePacer's own remarks describe: never fed back into scheduling.");

        private static readonly UpDownCounter<int>? ActiveSubscribersCounter = Telemetry.CreateUpDownCounter<int>(
            "thunderpropagator.videoplayer.media.subscribers.active", "{viewer}",
            "Currently subscribed viewers, summed across every session sharing this instance. Untagged — see this type's own remarks on why a session id is never a metric tag here.");

        private readonly int _frameSampleRate;
        private long _videoFrameSampleCounter;
        private long _audioFrameSampleCounter;

        /// <param name="frameSampleRate">
        /// Only every Nth publish per <see cref="VideoPlaybackMediaType"/> is offered to
        /// <see cref="StartSampledFrameActivity"/> — #235's own AC, "Sample or trace-gate per-frame logs."
        /// Must be strictly positive. Default 30 keeps a ~30fps video track's own sampled activity rate to
        /// about once a second without needing every caller to pick a value.
        /// </param>
        public VideoPlaybackTelemetry(int frameSampleRate = 30)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(frameSampleRate, 0);
            _frameSampleRate = frameSampleRate;
        }

        /// <summary>Records one frame successfully decoded from the source.</summary>
        public void RecordFrameDecoded(VideoPlaybackMediaType mediaType) => FramesDecodedCounter?.Add(1, MediaTypeTag(mediaType));

        /// <summary>Records one decode operation's wall-clock duration.</summary>
        public void RecordDecodeDuration(TimeSpan duration, VideoPlaybackMediaType mediaType) => DecodeDurationHistogram?.Record(duration.TotalMilliseconds, MediaTypeTag(mediaType));

        /// <summary>Records one frame successfully encoded for the wire.</summary>
        public void RecordFrameEncoded(VideoPlaybackMediaType mediaType) => FramesEncodedCounter?.Add(1, MediaTypeTag(mediaType));

        /// <summary>Records one encode operation's wall-clock duration.</summary>
        public void RecordEncodeDuration(TimeSpan duration, VideoPlaybackMediaType mediaType) => EncodeDurationHistogram?.Record(duration.TotalMilliseconds, MediaTypeTag(mediaType));

        /// <summary>Records one frame published (delivered to every currently subscribed viewer), including its encoded payload size.</summary>
        public void RecordFramePublished(VideoPlaybackMediaType mediaType, int payloadBytes)
        {
            FramesPublishedCounter?.Add(1, MediaTypeTag(mediaType));
            BytesPublishedCounter?.Add(payloadBytes, MediaTypeTag(mediaType));
        }

        /// <summary>Records the wall-clock cost of one publish operation (packet build + subscriber fan-out).</summary>
        public void RecordPublishLatency(TimeSpan duration, VideoPlaybackMediaType mediaType) => PublishLatencyHistogram?.Record(duration.TotalMilliseconds, MediaTypeTag(mediaType));

        /// <summary>Records how far a frame's actual publish moment drifted from its computed schedule — see <see cref="Media.FramePacer.GetPacingError"/>.</summary>
        public void RecordPacingDrift(TimeSpan drift, VideoPlaybackMediaType mediaType) => PacingDriftHistogram?.Record(drift.TotalMilliseconds, MediaTypeTag(mediaType));

        /// <summary>
        /// Records one frame a client has confirmed it rendered. Not yet called anywhere in this codebase
        /// — see <see cref="FramesAcknowledgedCounter"/>'s own description — but kept as a real public API
        /// so a future client-ack receive pipeline can wire it up without another metrics-design pass.
        /// </summary>
        public void RecordFrameAcknowledged(VideoPlaybackMediaType mediaType) => FramesAcknowledgedCounter?.Add(1, MediaTypeTag(mediaType));

        /// <summary>Records one frame dropped before publication, and why — pass straight through from a <see cref="FrameDropReason"/> callback.</summary>
        public void RecordFrameDropped(FrameDropReason reason, VideoPlaybackMediaType mediaType) =>
            FramesDroppedCounter?.Add(1, MediaTypeTag(mediaType), new KeyValuePair<string, object?>(DropReasonTagName, reason));

        /// <summary>Records one viewer subscribing for the first time (a reconnect/no-op re-subscribe never calls this — see <see cref="Media.Session.VideoPlaybackSession"/>'s own <c>RegisterSubscriber</c>).</summary>
        public void RecordSubscriberJoined() => ActiveSubscribersCounter?.Add(1);

        /// <summary>Records one previously-subscribed viewer leaving.</summary>
        public void RecordSubscriberLeft() => ActiveSubscribersCounter?.Add(-1);

        /// <summary>Records an unrecoverable session failure. <paramref name="stage"/> is a small fixed vocabulary ("open", "playback") the caller controls — never an exception message or other unbounded text.</summary>
        public void RecordSessionFailure(string stage) => SessionFailuresCounter?.Add(1, new KeyValuePair<string, object?>(FailureStageTagName, stage));

        /// <summary>
        /// Starts a sampled, per-frame diagnostic <see cref="Activity"/> carrying exactly the detail a raw
        /// metric tag must never carry — session id, epoch, frame number, media position — #235's own AC,
        /// "epoch, frame number, media position" alongside "Sample or trace-gate per-frame logs." Returns
        /// <see langword="null"/> (a no-op <c>using</c>) whenever nobody is listening
        /// (<see cref="Telemetry.HasListeners"/>) or this call falls outside the configured sample rate —
        /// the two checks together are this type's own "sample or trace-gate" behavior: near-zero cost on
        /// the hot path when no collector is attached, and only 1-in-<c>frameSampleRate</c> activities even
        /// when one is.
        /// </summary>
        public Activity? StartSampledFrameActivity(
            VideoPlaybackMediaType mediaType,
            string sessionId,
            int epoch,
            long frameNumber,
            TimeSpan mediaPosition,
            TimeSpan pacingDrift,
            TimeSpan publishLatency,
            int payloadBytes)
        {
            if (!Telemetry.HasListeners() || !ShouldSample(mediaType))
                return null;

            return Telemetry.StartActivity($"{nameof(VideoPlaybackTelemetry)}_Frame", ActivityKind.Producer)?
                .SetTag(MediaTypeTagName, mediaType)
                .SetTag("session.id", sessionId)
                .SetTag("epoch", epoch)
                .SetTag("frame.number", frameNumber)
                .SetTag("media.position.ms", mediaPosition.TotalMilliseconds)
                .SetTag("pacing.drift.ms", pacingDrift.TotalMilliseconds)
                .SetTag("publish.latency.ms", publishLatency.TotalMilliseconds)
                .SetTag("payload.bytes", payloadBytes);
        }

        private bool ShouldSample(VideoPlaybackMediaType mediaType)
        {
            var count = mediaType == VideoPlaybackMediaType.Video
                ? Interlocked.Increment(ref _videoFrameSampleCounter)
                : Interlocked.Increment(ref _audioFrameSampleCounter);

            return count % _frameSampleRate == 0;
        }

        private static KeyValuePair<string, object?> MediaTypeTag(VideoPlaybackMediaType mediaType) => new(MediaTypeTagName, mediaType);
    }
}
