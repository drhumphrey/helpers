# Milestone log

One entry per milestone: what was built, what was measured, what didn't go to plan. Newest at the bottom.

## Milestone 0: scaffolding (8 October 2026)

**Built:** `Helpers.slnx` with Core, Speech, Ai, Windows and App projects, an xUnit test project, shared build settings in `Directory.Build.props`, and a GitHub Actions workflow that builds and tests on every push.

**Not to plan:**

- The Avalonia template installed Avalonia 12, not 11. Same licence, brief updated.
- The .NET 10 SDK writes solutions as `.slnx`, so the file is `Helpers.slnx`.
- winget hung on this PC while refreshing its index, so the SDK was installed with Microsoft's direct installer.
- The checkout lives in Dropbox. `.git` and every `bin` and `obj` folder are marked Dropbox-ignored instead of moving the folder.

## Milestone 1: engine spike (8 October 2026)

**Built:** `tools/EngineSpike`, a console program that downloads the Kokoro model package once, loads it in sherpa-onnx, speaks a 325-character paragraph with two British voices, streams audio to the speakers as the engine produces it, saves WAV files, and prints the numbers below.

**Model:** `kokoro-multi-lang-v1_0` from the sherpa-onnx releases, 350 MB download, full precision. It is the only sherpa-onnx package with the British voices. No int8 build of it exists; the only int8 Kokoro package is v1.1, which is Chinese-focused with three English voices.

**Machine:** Dave's work PC, Windows 11, CPU only, 4 engine threads.

| Measure | Result | Brief target |
|---|---|---|
| Model load | 850 to 900 ms | not set |
| Time to first audio, first ever run | 855 ms | under 1 s |
| Time to first audio, warm | 130 to 230 ms | under 1 s |
| Synthesis speed | 6 to 8 times faster than real time (RTF 0.12 to 0.16) | not set |
| Memory after load | about 450 MB working set | under 400 MB |
| Memory after three paragraphs | about 700 MB working set, 860 MB private | under 400 MB |

**Voice quality:** Dave's call. The WAV files are in `out/engine-spike/` and the spike can be re-run with any of the eight British voices: `dotnet run --project tools/EngineSpike -- --runs bf_lily:1.0,bm_daniel:1.0`.

**Not to plan:**

- **Memory is over target and grows with use.** The engine's working set rises from 450 MB to 700 MB over three paragraphs, with playback switched off, so the growth is in onnxruntime, not the spike. Things to try at milestone 3: synthesise one sentence at a time rather than a whole paragraph (the arena grows with the longest input), look for an onnxruntime arena setting sherpa-onnx exposes, quantise the model to int8 ourselves, and rely on the planned "unload when idle" setting.
- **No int8 British model** exists ready-made, so the brief's "prefer int8" can't be followed yet.
- **NAudio 3 renamed things.** `WaveOutEvent` is now `WaveOut`, and the buffer size is set in the constructor. Worth knowing before milestone 3.
- **The spike targets `net10.0-windows`** because NAudio's playback classes only ship for Windows frameworks. The real `IAudioOutput` for Windows will need the same.
