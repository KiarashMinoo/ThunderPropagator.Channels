# ThunderPropagator.Channels.Demo.VideoPlayer

A demo channel that decodes a server-approved video (and, optionally, its audio track) with FFmpeg and
paces the output to a shared timeline for every subscribed viewer, with host-only playback controls,
late join, and lightweight reactions.

> **Client/UI status**: this package is server-side only. The runnable client example and its UI
> integration are tracked separately (see the parent epic). As of this package's own current version,
> nothing in this repository reads a session's decoded/encoded frame queues and streams them to a
> connection — `AddVideoPlayerChannel()` wires up channel state, session/decoder lifecycle, host
> commands, and the JSON state broadcast, but the binary frame *delivery* transport is not yet built.
> See [Known limitations](#known-limitations) below.

## Contents

- [Server architecture](#server-architecture)
- [Registration](#registration)
- [Configuration reference](#configuration-reference)
- [FFmpeg / native prerequisites](#ffmpeg--native-prerequisites)
- [Bandwidth and CPU cost](#bandwidth-and-cpu-cost)
- [Source allowlist](#source-allowlist)
- [Client protocol](#client-protocol)
- [Late join](#late-join)
- [Audio](#audio)
- [Security boundaries](#security-boundaries)
- [Troubleshooting](#troubleshooting)
- [Known limitations](#known-limitations)

## Server architecture

One `VideoPlaybackSession` per channel key owns exactly one active decode/publish pipeline
("generation") shared by every subscribed viewer — every viewer watches the same decode, not one each:

```
IVideoFrameSource (FFmpeg or synthetic)
        │  decoded frames
        ▼
DecodedFrameBuffer (bounded; newest-due-frame wins on overflow)
        │  paced by FramePacer (monotonic-clock scheduling, drift-free)
        ▼
VideoFrameEncoder (JPEG/WebP)  ──publish──▶  one SubscriberFrameQueue<VideoFramePacket> per viewer
```

The audio path (only active when both `EnableAudio` is `true` and a source has an audio track) mirrors
this exactly, sharing the same `FramePacer`/epoch/generation, so audio and video packets always carry
synchronized timestamps:

```
IAudioFrameSource ─▶ DecodedAudioBuffer ─▶ AudioFrameEncoder (Opus/AAC) ─▶ per-viewer audio queue
```

`VideoPlaybackSessionManager` is the thread-safe, keyed (by channel key) collection of active sessions.
`VideoPlaybackSession.Select`/`Seek` open a fresh source per "generation" (never reusing a source across
a seek) and increment an `Epoch`; every packet carries that epoch, so a client (or a server-side
consumer) can always recognize and discard a stale, superseded-generation packet. A viewer's own queue
never blocks the publisher or any other viewer — a slow viewer only ever drops its own oldest frame.

Every buffer/queue is strictly bounded (`DecodeBufferCapacity`, `SubscriberQueueCapacity`,
`AudioDecodeBufferCapacity`, `AudioSubscriberQueueCapacity`) — there is no unbounded backlog anywhere in
this pipeline.

## Registration

```csharp
services.AddVideoPlayerChannel(configuration =>
{
    configuration.PlaylistPolicy = new VideoPlaylistPolicy
    {
        LocalFileRoot = "/srv/approved-videos"
    };
    configuration.PlaylistEntries =
    [
        new VideoPlaylistEntry
        {
            VideoId = "intro",
            Title = "Welcome",
            Source = new VideoSource { Location = "/srv/approved-videos/intro.mp4" }
        }
    ];
});
```

One call registers everything this channel needs: the channel itself, every `Video/*` receive pipeline
(`Video/React` only if `EnableReactions` stays `true`), the shared `VideoPlaybackSessionManager`
(constructing real `FfmpegVideoFrameSource`/`FfmpegAudioFrameSource` instances, bounded by
`SourceOpenTimeout`), the approved-video `IVideoPlaylist`, and process-wide `VideoPlaybackTelemetry`
(see that type's own XML docs for the metric/trace catalogue it publishes). Calling
`AddVideoPlayerChannel` more than once on the same `IServiceCollection` is a safe no-op — the first
call's configuration wins.

Every setting is validated the moment registration runs — a misconfigured value (an out-of-range
dimension/quality/timeout, an unsupported encoding, a `DefaultVideoId` that doesn't resolve to a known
enabled playlist entry, a playlist entry whose source violates `PlaylistPolicy`) throws immediately,
failing host startup with a property-specific message rather than surfacing later as a confusing
runtime failure.

## Configuration reference

Every setting lives on `VideoPlayerChannelConfiguration` (see that type's own XML docs for the
authoritative per-property detail — defaults, valid ranges, and exactly what each one costs):

| Setting | What it controls |
|---|---|
| `SessionId` | A stable session id, in place of the runtime-generated channel key. |
| `DefaultVideoId` | Reserved for a future auto-select-on-start behavior; validated against the playlist today, not yet acted on. |
| `PlaylistEntries` / `PlaylistPolicy` | The approved-video allow-list — see [Source allowlist](#source-allowlist). |
| `MaxWidth` / `MaxHeight` | Output frame dimensions (scaled down, never up). Higher costs more encode CPU and bandwidth per frame. |
| `Encoding` / `Quality` | Wire codec (JPEG/WebP) and encode quality (0-100). Higher quality costs more bytes per frame. |
| `EnableAudio` / `AudioEncoding` / `AudioBitRate` | Whether/how the audio track is encoded. |
| `EnableReactions` / `AllowedReactions` / `ReactionWindow` / `MaxReactionsPerViewerPerWindow` | `Video/React`'s own vocabulary and per-viewer rate limit. |
| `DecodeBufferCapacity` / `SubscriberQueueCapacity` / `AudioDecodeBufferCapacity` / `AudioSubscriberQueueCapacity` | Bounded buffer/queue depths. Higher tolerates more jitter, at the cost of more memory per session/viewer. |
| `PlaybackRate` | Speed multiplier (1.0 = real-time). |
| `PollInterval` | How often the publish loop checks for a due frame. Lower costs more CPU. |
| `SourceOpenTimeout` | How long `OpenAsync` may take before it's treated as failed. |
| `MaxPublishLatenessBeforeBuffering` | Reserved for a future `Buffering`-state transition; validated today, not yet enforced. |
| `IdleSessionRetention` | Reserved for a future automatic idle-session cleanup sweep; validated today, not yet enforced. |

## FFmpeg / native prerequisites

The FFmpeg-backed source/decoder (`FfmpegVideoFrameSource`/`FfmpegAudioFrameSource`) P/Invokes into
FFmpeg's shared libraries (`avformat`, `avcodec`, `avutil`, `swscale`, `swresample`) via `FFmpeg.AutoGen`.
They are **not bundled** with this package and must be present on the host separately:

- **Windows**: the matching-architecture (x64/ARM64) FFmpeg shared build's `.dll` files, either on `PATH`
  or under a directory you set as `FfmpegVideoFrameSourceOptions.RootPath`.
- **Linux**: the distribution's `ffmpeg`/`libav*` shared libraries (e.g. Debian/Ubuntu's `libavformat*.so`
  family), discoverable via `ldconfig`/`LD_LIBRARY_PATH`.
- **macOS**: FFmpeg's `.dylib` files (e.g. via Homebrew's `ffmpeg` formula), discoverable via
  `DYLD_LIBRARY_PATH`.

The libraries load lazily on first use — a missing or mismatched-architecture library surfaces as a
`VideoFrameSourceException` the first time a session actually tries to open a source, not at process
startup. See the opt-in integration test suite
(`Tests/.../Demo/VideoPlayer/Media/Video/FfmpegVideoFrameSourceFixtureTests.cs` and its own
`Fixtures/README.md`) for how to exercise the real decoder locally.

## Bandwidth and CPU cost

Every viewer of one session receives the *same* encoded frames (one decode, N deliveries) — cost does
not scale per-viewer for decode/encode, only for delivery once a transport exists (see
[Known limitations](#known-limitations)). Per session, roughly:

- **CPU**: one decode thread's worth of FFmpeg decoding, one scale (`libswscale`) per frame to
  `MaxWidth`×`MaxHeight`, and one JPEG/WebP encode per frame at `Quality`. Higher `MaxWidth`/`MaxHeight`
  or `Quality` costs more of both; lower `PollInterval` adds a small, constant polling overhead on top.
- **Memory**: bounded by `DecodeBufferCapacity` decoded (raw pixel) frames server-side, plus
  `SubscriberQueueCapacity` already-encoded packets *per viewer* — memory grows with viewer count only
  in the (small, bounded) per-viewer queue, never in decode/encode.
- **Per-frame bytes**: roughly proportional to `MaxWidth` × `MaxHeight` × `Quality`; JPEG/WebP compression
  ratio depends heavily on source content. Measure with real content at your target settings before
  sizing bandwidth for a deployment — this package does not estimate it for you.

`VideoPlaybackTelemetry` (registered automatically) publishes decode/encode/publish duration histograms,
pacing drift, bytes published, and frame-dropped counters (tagged by reason) so you can observe actual
cost/behavior in your own environment rather than relying on the estimates above.

## Source allowlist

**A client never supplies a path or URL.** Every `Video/Select` request carries only a `VideoId` — a
short, client-safe string resolved server-side against the `IVideoPlaylist` built from
`PlaylistEntries`/`PlaylistPolicy`. An unknown or disabled id is rejected identically (a client can never
distinguish "doesn't exist" from "exists but disabled"), and the actual `VideoSource.Location` a
`VideoId` resolves to is never sent to any client — see [Security boundaries](#security-boundaries).

`PlaylistPolicy` is deny-by-default and validated at registration time (a misconfigured entry fails host
startup, not a later request):

- `AllowedSchemes` — which source schemes are permitted at all (default: only `"file"`).
- `LocalFileRoot` — required for any local-file entry to validate; every local entry's path must resolve
  (after collapsing `..`/`.` segments) inside this directory, with a proper path-segment boundary check
  (not a naive string prefix, which a sibling directory could defeat).
- `AllowedRemoteHosts` — explicit host allow-list for any non-`"file"` scheme (default: empty, i.e. no
  remote source is ever approved). As defense-in-depth even for an allow-listed host, a literal IP
  address that is loopback, private-range (RFC 1918), or link-local (including the cloud metadata
  endpoint `169.254.169.254`) is rejected regardless — an SSRF backstop.

## Client protocol

### State broadcast (`VideoPlayerChannelFeederMessage`, JSON)

Every state-changing event broadcasts this to every subscriber: `SessionId`, `VideoId`/`Title` (the
approved playlist entry's own client-safe id/title — **never** the source path/URL), `State`
(`Loading`/`Playing`/`Paused`/`Buffering`/`Ended`/`Faulted`), `Epoch`, `CurrentFrameNumber`,
`MediaPosition`/`SyncTime` (microseconds — enough for a client to calculate the expected live position
while playing), `Host` (the connection currently authorized for host-only commands), `ViewerCount`,
`Duration`, `SourceFrameRate`, and `Reactions` (current aggregate counts).

### Host commands

All require `RequestType` set to the given key; all except `Video/Join`/`Video/React` are **host-only**
(the calling connection must be the session's current authorized host — see
[Security boundaries](#security-boundaries)) and reject a non-host caller with `403 Forbidden`:

| RequestType | Request fields | Notes |
|---|---|---|
| `Video/Select` | `VideoId`, optional `StartPositionMicroseconds` | Switches to a different approved video. Valid from any state, including the very first selection. Broadcasts `Loading`, then `Playing`/`Faulted`. |
| `Video/Play` | *(none)* | Resumes a paused timeline. |
| `Video/Pause` | *(none)* | Freezes the shared timeline for every viewer. |
| `Video/Seek` | `PositionMicroseconds` | Re-seeks the current video; clamped to the known duration, not rejected for an out-of-range value. Increments `Epoch`. |
| `Video/Join` | *(none — not host-only)* | Subscribes the caller and atomically bootstraps it with whatever frame was most recently published (if any), so a viewer joining mid-playback starts at the live position, not frame 0. Response reports `IsReconnect` so a client can reset local state on an unexpected rejoin. |
| `Video/React` | `Reaction` | Not host-only — any subscribed viewer may react. Validated against `AllowedReactions`, length, and a per-viewer rate limit (`ReactionWindow`/`MaxReactionsPerViewerPerWindow`); an invalid/disallowed reaction or a caller over their own rate limit is rejected. Response reports the resulting aggregate counts. |

Host authorization is automatic, not claimed: the first eligible subscriber becomes host on
`Join`/subscribe; if the host disconnects, host reassigns deterministically to whichever remaining
subscriber joined earliest. There is no way for a connection to claim host status by asserting an id —
only actual subscription order decides it.

### Binary frame packets

Decoded, encoded frames are transported as length-prefixed binary (`VideoFramePacket`/
`VideoFramePacketSerializer`, and `AudioFramePacket`/`AudioFramePacketSerializer` for audio) — entirely
separate from the JSON state broadcast above. Every packet carries `SessionId`, `Epoch`, a monotonically
increasing frame/packet number within that epoch, `PresentationTimestamp`/`Duration`, `DisplayTime`
(server-scheduled display time, accounting for pacing), and the already-encoded payload bytes. **A
packet never carries anything that identifies the underlying source** — only its own frame data and the
same `VideoId`-free identifiers the JSON state already uses.

As of this package's current version, packets are produced and queued per-viewer server-side but not yet
delivered to a connection — see [Known limitations](#known-limitations).

## Late join

`Video/Join` is how a viewer joining mid-playback (or reconnecting) starts at the current live position
instead of frame 0, without creating a second decoder or timeline — every viewer of one session always
shares the exact same decode. The bootstrap is atomic relative to the publish loop: a join can never
race a concurrent publish into either duplicating a frame or momentarily rewinding what a viewer sees.
Joining while `Paused` delivers the paused frame and nothing further until `Video/Play` resumes.

## Audio

Active only when both `EnableAudio` is `true` (default) *and* the selected source actually has an audio
track — a source with no audio, or whose audio track fails to open, still plays video-only rather than
failing the whole session (audio is always a best-effort value-add over video, never the other way
around). Codec selection: `AudioEncoding` left `null` (the default) auto-detects per source — an
already-AAC source encodes to AAC (avoiding a redundant lossy transcode and favoring broader legacy
compatibility), everything else encodes to Opus. Set `AudioEncoding` explicitly to force one codec
regardless of source. Audio and video packets always share the same session epoch and a synchronized
clock, so a client never has to reconcile two independent timelines.

## Security boundaries

- **The original source location is never exposed.** `VideoSource.Location` (a file path, URL, or
  decoder-specific connection string) exists only server-side, inside `VideoPlaylistEntry.Source` and
  the FFmpeg-backed decoder it's handed to. No client-facing DTO, the JSON state broadcast, a binary
  frame packet, an exception message, or a log line emitted by this package's own code ever includes it
  — every client-visible identifier is the approved-playlist `VideoId` (and its `Title`), never the
  underlying MP4/HLS/file path/URL itself. (`VideoPlaybackTelemetry`'s own structured logging was
  specifically verified against this: `FfmpegVideoFrameSource`'s own exception messages never
  interpolate `VideoSource.Location`, only a generic FFmpeg error description — see that type's own
  remarks.)
- **A client can never reach the decoder with an arbitrary path/URL.** `Video/Select` accepts only a
  `VideoId`, resolved against the server's own allow-list — see [Source allowlist](#source-allowlist).
- **Host-only commands are authorization-checked server-side on every call**, not merely hidden client-side
  — see [Client protocol](#client-protocol).
- **Metric tags stay bounded** (media type, drop reason, a small failure-stage vocabulary) — a session id,
  viewer id, or frame number is never a raw metric tag (which would be both a cardinality blowup and an
  unnecessary identifier leak into telemetry); that level of per-instance detail rides on a
  sampled/trace-gated diagnostic activity instead, the correct place for it.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `VideoFrameSourceException` from `OpenAsync`, mentioning a missing/mismatched library | FFmpeg native shared libraries not installed, not on the library search path, or wrong architecture — see [FFmpeg / native prerequisites](#ffmpeg--native-prerequisites). |
| `OpenAsync` fails after roughly `SourceOpenTimeout` | The source is slow/unreachable and hit the configured timeout — check the source itself (network path, disk), or raise `SourceOpenTimeout` for a genuinely slow-but-healthy source. |
| `Video/Select` rejected with "video not available" | `VideoId` doesn't resolve to a known, *enabled* playlist entry — check `PlaylistEntries` and each entry's `IsEnabled`. |
| Registration throws `VideoPlaylistValidationException` at startup | A `PlaylistEntries` entry's `Source` doesn't satisfy `PlaylistPolicy` (wrong scheme, outside `LocalFileRoot`, host not in `AllowedRemoteHosts`, or a private/loopback/link-local remote host) — see [Source allowlist](#source-allowlist). |
| Frequent `DecodeBufferCapacityExceeded`/`SubscriberQueueCapacityExceeded` drops | Decode or publish is outpacing the configured buffer/queue depth for real content at your current settings — raise `DecodeBufferCapacity`/`SubscriberQueueCapacity`, lower `MaxWidth`/`MaxHeight`/`Quality`, or investigate host CPU pressure. `VideoPlaybackTelemetry`'s own dropped-frame counter (tagged by reason) tells you which buffer is actually overflowing. |
| High CPU/bandwidth for the settings you expected | See [Bandwidth and CPU cost](#bandwidth-and-cpu-cost) — measure with `VideoPlaybackTelemetry`'s own histograms against your real content rather than assuming from dimensions/quality alone. |
| A command silently no-ops or 403s unexpectedly | Confirm the calling connection is actually this session's current host (`Video/Play`/`Pause`/`Seek`/`Select` are host-only) — see [Client protocol](#client-protocol). |

## Known limitations

- **No binary frame delivery transport yet** (see the note at the top of this document) — decode,
  encode, pacing, per-viewer queueing, and host commands are all live and tested; actually streaming the
  queued packets to a connection is not yet implemented anywhere in this repository.
- `MaxPublishLatenessBeforeBuffering` and `IdleSessionRetention` are validated but not yet enforced — no
  session transitions into `Buffering` today, and no automatic idle-session cleanup sweep runs yet.
- `DefaultVideoId` is validated against the playlist at registration time but nothing currently calls
  `Video/Select` automatically on startup/first-join using it.
