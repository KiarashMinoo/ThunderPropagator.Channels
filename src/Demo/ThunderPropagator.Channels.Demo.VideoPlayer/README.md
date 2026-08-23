# ThunderPropagator.Channels.Demo.VideoPlayer

A server-driven, frame-by-frame cinema streaming demo channel. The server owns the media source,
decoder, media clock, pacing, seeking, and the shared session timeline; clients receive independently
renderable binary frame/audio packets and render them at their presentation times. Clients never
receive an MP4/HLS/source URL and never independently play the original source.

This project is currently a scaffold (#213): the mandatory channel unit exists and builds, but the
media pipeline itself — frame/audio contracts, the decoder, session and pacing services, and the
`Video/*` command pipelines — is implemented by the child issues tracked under the parent epic (#6).

## Native dependency

The frame decoder (#217) will depend on a native FFmpeg installation on the host machine — this
scaffold introduces no such dependency yet (no FFmpeg package reference exists in
`Directory.Packages.props`), but any environment that later builds or runs this channel's decoder
should plan for an FFmpeg native binary (or wrapper package) to be present at runtime. That
dependency, and how to provision it, is documented fully once #217 lands.
