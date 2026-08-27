# VideoPlayer opt-in FFmpeg integration fixtures (#237)

This repo's CI/dev environment has no native FFmpeg shared libraries installed, so
`FfmpegVideoFrameSource`/`FfmpegAudioFrameSource` can only be exercised against **real** media on a
machine that has FFmpeg available. The tests that do this are opt-in: they no-op (pass without
asserting anything) unless the environment variables below point at real files.

## Provenance and license

Every fixture is generated locally by `Generate-VideoPlayerFixtures.ps1` (in this same folder) using
only FFmpeg's own built-in synthetic source filters — `testsrc2` for video, `sine` for audio. Nothing
is downloaded, recorded, ripped, or copied from any external source. The generated files contain no
third-party content of any kind, so there is no license to track beyond FFmpeg's own (the fixtures are
never distributed — they exist only on whichever machine ran the script, and are `.gitignore`'d, never
committed to this repository).

## Generating the fixtures

Requires the `ffmpeg` CLI on `PATH` (any recent build with `libx264`/`aac` and the `lavfi` source
filters — the default on most package managers).

```powershell
./Generate-VideoPlayerFixtures.ps1
```

This writes `generated/cfr-fixture.mp4` (25fps constant frame rate, 2s, video + a synthetic 440Hz
audio tone) and `generated/vfr-fixture.mp4` (variable frame rate, video-only, 2s) into a `generated/`
subfolder next to the script — already excluded by this repo's `.gitignore`.

## Running the opt-in tests

Set the environment variable(s) for whichever fixture(s) you generated, then run the normal test
suite (only the tests below check these variables — everything else runs exactly as it always does):

| Variable | Points at | Consumed by |
|---|---|---|
| `THUNDERPROPAGATOR_VIDEOPLAYER_CFR_FIXTURE` | `cfr-fixture.mp4` | `FfmpegVideoFrameSourceFixtureTests` (CFR cases) |
| `THUNDERPROPAGATOR_VIDEOPLAYER_VFR_FIXTURE` | `vfr-fixture.mp4` | `FfmpegVideoFrameSourceFixtureTests` (VFR cases) |
| `THUNDERPROPAGATOR_VIDEOPLAYER_AV_FIXTURE` | `cfr-fixture.mp4` (it carries both tracks) | `AudioVideoSyncFixtureTests` (#224) |

```powershell
$env:THUNDERPROPAGATOR_VIDEOPLAYER_CFR_FIXTURE = (Resolve-Path ./generated/cfr-fixture.mp4)
$env:THUNDERPROPAGATOR_VIDEOPLAYER_VFR_FIXTURE = (Resolve-Path ./generated/vfr-fixture.mp4)
$env:THUNDERPROPAGATOR_VIDEOPLAYER_AV_FIXTURE   = (Resolve-Path ./generated/cfr-fixture.mp4)
dotnet test --filter "FullyQualifiedName~Fixture"
```

Unset (or pointing at a missing file), every one of these tests is a no-op pass — this repo's normal
`dotnet test` run is completely unaffected, on any machine, with or without FFmpeg installed.

## CI

`.github/workflows/ffmpeg-integration.yml` runs this same flow as a manually-triggered
(`workflow_dispatch`) job: install `ffmpeg`, run this script, set the three environment variables
above, and run only the fixture-gated tests. It never runs on a normal push/PR — the main `ci.yml`
pipeline has no FFmpeg available and doesn't need it, since every other VideoPlayer test in this repo
is already deterministic against the synthetic sources under `../` (no real FFmpeg needed).
