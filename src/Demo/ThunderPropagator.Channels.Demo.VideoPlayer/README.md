# ThunderPropagator.Channels.Demo.VideoPlayer

A server-driven, frame-by-frame cinema streaming demo channel. The server owns the media source,
decoder, media clock, pacing, seeking, and the shared session timeline; clients receive independently
renderable binary frame/audio packets and render them at their presentation times. Clients never
receive an MP4/HLS/source URL and never independently play the original source.

The channel/message/metadata contracts (#213/#215), the binary frame transport (#214), the replaceable
`IVideoFrameSource` abstraction (#216), and the FFmpeg-backed decoder/encoder (#217) are implemented.
Session/pacing services and the `Video/*` command pipelines are implemented by the remaining child
issues tracked under the parent epic (#6).

## Native dependency: FFmpeg

`ThunderPropagator.Channels.Demo.VideoPlayer.Media.FfmpegVideoFrameSource` decodes video via
`FFmpeg.AutoGen`, a P/Invoke binding to FFmpeg's native `libav*`/`libswscale` shared libraries.
**Those native libraries are not bundled with this package** — install them separately on every
machine that opens a real video source (a machine that only ever uses `IVideoFrameSource`'s own
synthetic test double, or the JPEG/WebP `VideoFrameEncoder` on frames from some other source, needs
none of this):

| Platform | What to install | How this package finds it |
|---|---|---|
| Windows | A matching-architecture (x64/ARM64) FFmpeg *shared* build's `avformat-*.dll`, `avcodec-*.dll`, `avutil-*.dll`, `swscale-*.dll` (and their own dependencies) | `PATH`, or a directory set via `FfmpegVideoFrameSourceOptions.RootPath` |
| Linux | The distribution's `ffmpeg`/`libav*` shared libraries (e.g. Debian/Ubuntu's `libavformat*` package family) | The dynamic linker (`ldconfig`/`LD_LIBRARY_PATH`), or `FfmpegVideoFrameSourceOptions.RootPath` |
| macOS | FFmpeg's `.dylib` files (e.g. via Homebrew's `ffmpeg` formula) | `DYLD_LIBRARY_PATH`, or `FfmpegVideoFrameSourceOptions.RootPath` |

The libraries load lazily — constructing `FfmpegVideoFrameSource` never touches them; only the first
call to `OpenAsync` does. A missing or mismatched-architecture library surfaces as a
`VideoFrameSourceException` (or, for a total load failure the binding itself can't wrap, a raw
`DllNotFoundException`) at that point, not as a crash at process startup.

This repository's own CI/dev environment does not have FFmpeg installed, so `FfmpegVideoFrameSource`
is verified here only by compilation and by the parts of it that don't need FFmpeg at all —
`VideoFrameScaling`'s aspect-ratio math and `VideoFrameEncoder`'s JPEG/WebP encoding (pure SkiaSharp,
fully exercised by real encode/decode round-trip tests). Real decode correctness against a checked-in
or generated media fixture is #237's own scope, "opt-in FFmpeg integration coverage with a local media
fixture," to run wherever FFmpeg is actually available.
