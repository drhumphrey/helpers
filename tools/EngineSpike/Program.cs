// Engine spike: milestone 1 of Helpers.
//
// Reads a paragraph aloud with Kokoro running in sherpa-onnx, entirely offline,
// and reports the three numbers the brief asks for: model load time, time to
// first audio, and memory use. It also saves each run as a WAV file so the
// voices can be compared later without re-running.
//
// Usage:
//   dotnet run --project tools/EngineSpike -- [options]
//
//   --models <dir>   Where model packages live. Default: %LOCALAPPDATA%\Helpers\models
//   --out <dir>      Where to save WAV files.     Default: %LOCALAPPDATA%\Helpers\spike
//   --runs <list>    Voice:speed pairs.           Default: bf_emma:1.0,bm_george:1.5,bf_emma:1.0
//   --text <file>    Read this file instead of the built-in paragraph.
//   --threads <n>    Threads for the engine.      Default: 4
//   --no-play        Synthesize and save only; don't play audio.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using NAudio.Wave;
using SherpaOnnx;

namespace EngineSpike;

internal static class Program
{
    // The only sherpa-onnx Kokoro package that contains the British voices.
    private const string PackageName = "kokoro-multi-lang-v1_0";
    private const string DownloadUrl =
        "https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/" + PackageName + ".tar.bz2";

    // Speaker IDs inside that package, from the sherpa-onnx documentation.
    private static readonly Dictionary<string, int> BritishVoices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bf_alice"] = 20,
        ["bf_emma"] = 21,
        ["bf_isabella"] = 22,
        ["bf_lily"] = 23,
        ["bm_daniel"] = 24,
        ["bm_fable"] = 25,
        ["bm_george"] = 26,
        ["bm_lewis"] = 27,
    };

    // Deliberately in the style of an AI coding assistant's reply, with the
    // abbreviations and numbers that trip up simple sentence splitters.
    private const string DefaultParagraph =
        "Here's what I found. The build fails because the test project targets .NET 8, " +
        "while the core library targets .NET 10. Change the target framework in the test " +
        "project, then run the tests again. Dr. Patel's note about Fig. 2 is unrelated, " +
        "e.g. it concerns the 2.5 second timeout. Once that's done, version 0.1 is ready to ship.";

    private static async Task<int> Main(string[] args)
    {
        var options = Options.Parse(args);
        if (options is null)
        {
            return 1;
        }

        var modelDir = Path.Combine(options.ModelsRoot, PackageName);
        Directory.CreateDirectory(options.ModelsRoot);
        Directory.CreateDirectory(options.OutDir);

        try
        {
            await EnsureModelAsync(options.ModelsRoot, modelDir);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not get the voice model: {ex.Message}");
            return 2;
        }

        var text = options.TextFile is null ? DefaultParagraph : await File.ReadAllTextAsync(options.TextFile);

        Console.WriteLine();
        Console.WriteLine($"Engine:  sherpa-onnx, {options.Threads} threads, CPU");
        Console.WriteLine($"Model:   {modelDir}");
        Console.WriteLine($"Text:    {text.Length} characters");
        Console.WriteLine();

        var before = Memory.Snapshot();
        var loadWatch = Stopwatch.StartNew();
        OfflineTts tts;
        try
        {
            tts = LoadEngine(modelDir, options.Threads);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"The engine failed to load: {ex.Message}");
            return 3;
        }

        loadWatch.Stop();
        var afterLoad = Memory.Snapshot();

        Console.WriteLine($"Model load time:        {loadWatch.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"Memory before load:     {before}");
        Console.WriteLine($"Memory after load:      {afterLoad}");
        Console.WriteLine($"Sample rate:            {tts.SampleRate} Hz");
        Console.WriteLine();

        var results = new List<RunResult>();
        foreach (var (voice, speed) in options.Runs)
        {
            if (!BritishVoices.TryGetValue(voice, out var sid))
            {
                Console.Error.WriteLine($"Unknown voice '{voice}'. Known: {string.Join(", ", BritishVoices.Keys)}");
                return 1;
            }

            var result = Speak(tts, text, voice, sid, speed, options);
            results.Add(result);
            Console.WriteLine(result);
            Console.WriteLine();
        }

        Console.WriteLine($"Memory after all runs:  {Memory.Snapshot()}");
        Console.WriteLine($"WAV files saved in:     {options.OutDir}");
        Console.WriteLine();
        Console.WriteLine("Summary (voice, speed, load ms, first audio ms, synth ms, audio s, real-time factor):");
        foreach (var r in results)
        {
            Console.WriteLine(
                $"  {r.Voice,-12} {r.Speed:0.0}x  first audio {r.FirstAudioMs,5:N0} ms   " +
                $"synth {r.SynthMs,6:N0} ms   audio {r.AudioSeconds,5:0.0} s   RTF {r.RealTimeFactor:0.00}");
        }

        return 0;
    }

    private static OfflineTts LoadEngine(string modelDir, int threads)
    {
        var config = new OfflineTtsConfig();
        config.Model.Kokoro.Model = Path.Combine(modelDir, "model.onnx");
        config.Model.Kokoro.Voices = Path.Combine(modelDir, "voices.bin");
        config.Model.Kokoro.Tokens = Path.Combine(modelDir, "tokens.txt");
        config.Model.Kokoro.DataDir = Path.Combine(modelDir, "espeak-ng-data");
        config.Model.Kokoro.Lexicon = Path.Combine(modelDir, "lexicon-gb-en.txt");

        var dictDir = Path.Combine(modelDir, "dict");
        if (Directory.Exists(dictDir))
        {
            config.Model.Kokoro.DictDir = dictDir;
        }

        config.Model.NumThreads = threads;
        config.Model.Provider = "cpu";
        config.Model.Debug = 0;

        return new OfflineTts(config);
    }

    /// <summary>
    /// Synthesizes the text with one voice and speed, streaming each chunk to the
    /// speakers as soon as the engine produces it, and measures the result.
    /// </summary>
    private static RunResult Speak(OfflineTts tts, string text, string voice, int sid, float speed, Options options)
    {
        Console.WriteLine($"--- {voice} (speaker {sid}) at {speed:0.0}x ---");

        var sampleRate = tts.SampleRate;
        var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        var buffer = new BufferedWaveProvider(format, TimeSpan.FromMinutes(10))
        {
            DiscardOnBufferOverflow = false,
            ReadFully = true,
        };

        using var output = options.Play ? new WaveOut() : null;
        output?.Init(buffer);

        var watch = Stopwatch.StartNew();
        long firstAudioMs = -1;
        var chunks = 0;
        long samplesSoFar = 0;

        var callback = new OfflineTtsCallbackProgressWithArg((IntPtr samples, int count, float progress, IntPtr arg) =>
        {
            if (firstAudioMs < 0)
            {
                firstAudioMs = watch.ElapsedMilliseconds;
                output?.Play();
            }

            chunks++;
            samplesSoFar += count;

            if (output is not null)
            {
                var floats = new float[count];
                Marshal.Copy(samples, floats, 0, count);
                var bytes = new byte[count * sizeof(float)];
                Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
                buffer.AddSamples(bytes, 0, bytes.Length);
            }

            Console.WriteLine($"  chunk {chunks}: {count:N0} samples at {watch.ElapsedMilliseconds:N0} ms ({progress:P0})");
            return 1; // keep going
        });

        var generation = new OfflineTtsGenerationConfig
        {
            Sid = sid,
            Speed = speed,
        };

        var audio = tts.GenerateWithConfig(text, generation, callback);
        watch.Stop();
        var synthMs = watch.ElapsedMilliseconds;

        var audioSeconds = audio.Samples.Length / (double)sampleRate;
        var wavPath = Path.Combine(options.OutDir, $"{voice}-{speed.ToString("0.0", CultureInfo.InvariantCulture)}x.wav");
        audio.SaveToWaveFile(wavPath);

        if (output is not null)
        {
            // Let the rest of the audio play out before the next run starts.
            while (buffer.BufferedBytes > 0)
            {
                Thread.Sleep(50);
            }

            Thread.Sleep(200);
            output.Stop();
        }

        return new RunResult(voice, speed, firstAudioMs, synthMs, audioSeconds, Memory.Snapshot());
    }

    /// <summary>Downloads and unpacks the model package if it isn't there already.</summary>
    private static async Task EnsureModelAsync(string modelsRoot, string modelDir)
    {
        if (File.Exists(Path.Combine(modelDir, "model.onnx")))
        {
            return;
        }

        var archive = Path.Combine(modelsRoot, PackageName + ".tar.bz2");
        if (!File.Exists(archive))
        {
            Console.WriteLine($"Downloading {PackageName} (about 350 MB, one time only)...");
            await DownloadAsync(DownloadUrl, archive);
        }

        Console.WriteLine("Unpacking...");
        var tar = Path.Combine(Environment.SystemDirectory, "tar.exe");
        if (!File.Exists(tar))
        {
            tar = "tar";
        }

        var unpack = Process.Start(new ProcessStartInfo(tar, $"-xf \"{archive}\" -C \"{modelsRoot}\"")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("tar could not be started.");

        var errors = await unpack.StandardError.ReadToEndAsync();
        await unpack.WaitForExitAsync();
        if (unpack.ExitCode != 0 || !File.Exists(Path.Combine(modelDir, "model.onnx")))
        {
            throw new InvalidOperationException($"tar failed (exit {unpack.ExitCode}): {errors}");
        }

        File.Delete(archive);
        Console.WriteLine("Model ready.");
    }

    private static async Task DownloadAsync(string url, string destination)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1;
        var partial = destination + ".part";
        await using (var source = await response.Content.ReadAsStreamAsync())
        await using (var file = File.Create(partial))
        {
            var chunk = new byte[1 << 16];
            long done = 0;
            var lastShown = -1;
            int read;
            while ((read = await source.ReadAsync(chunk)) > 0)
            {
                await file.WriteAsync(chunk.AsMemory(0, read));
                done += read;
                if (total > 0)
                {
                    var percent = (int)(done * 100 / total);
                    if (percent / 5 != lastShown / 5)
                    {
                        Console.WriteLine($"  {percent}% ({done / 1_048_576} of {total / 1_048_576} MB)");
                        lastShown = percent;
                    }
                }
            }
        }

        File.Move(partial, destination, overwrite: true);
    }

    private sealed record RunResult(
        string Voice,
        float Speed,
        long FirstAudioMs,
        long SynthMs,
        double AudioSeconds,
        Memory MemoryAfter)
    {
        public double RealTimeFactor => AudioSeconds <= 0 ? 0 : SynthMs / 1000.0 / AudioSeconds;

        public override string ToString() =>
            $"Time to first audio:    {FirstAudioMs:N0} ms\n" +
            $"Total synthesis time:   {SynthMs:N0} ms for {AudioSeconds:0.0} s of audio (RTF {RealTimeFactor:0.00})\n" +
            $"Memory after run:       {MemoryAfter}";
    }

    private readonly record struct Memory(long WorkingSetMb, long PrivateMb)
    {
        public static Memory Snapshot()
        {
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            return new Memory(process.WorkingSet64 / 1_048_576, process.PrivateMemorySize64 / 1_048_576);
        }

        public override string ToString() => $"working set {WorkingSetMb:N0} MB, private {PrivateMb:N0} MB";
    }

    private sealed class Options
    {
        public string ModelsRoot { get; private set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Helpers", "models");

        public string OutDir { get; private set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Helpers", "spike");

        public List<(string Voice, float Speed)> Runs { get; } = [];
        public string? TextFile { get; private set; }
        public int Threads { get; private set; } = 4;
        public bool Play { get; private set; } = true;

        public static Options? Parse(string[] args)
        {
            var options = new Options();
            var runs = "bf_emma:1.0,bm_george:1.5,bf_emma:1.0";

            for (var i = 0; i < args.Length; i++)
            {
                string Next()
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException($"{args[i]} needs a value.");
                    }

                    return args[++i];
                }

                try
                {
                    switch (args[i])
                    {
                        case "--models": options.ModelsRoot = Path.GetFullPath(Next()); break;
                        case "--out": options.OutDir = Path.GetFullPath(Next()); break;
                        case "--runs": runs = Next(); break;
                        case "--text": options.TextFile = Path.GetFullPath(Next()); break;
                        case "--threads": options.Threads = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                        case "--no-play": options.Play = false; break;
                        default:
                            Console.Error.WriteLine($"Unknown option {args[i]}.");
                            return null;
                    }
                }
                catch (Exception ex) when (ex is ArgumentException or FormatException)
                {
                    Console.Error.WriteLine(ex.Message);
                    return null;
                }
            }

            foreach (var run in runs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = run.Split(':');
                var speed = parts.Length > 1 ? float.Parse(parts[1], CultureInfo.InvariantCulture) : 1.0f;
                options.Runs.Add((parts[0], speed));
            }

            return options;
        }
    }
}
