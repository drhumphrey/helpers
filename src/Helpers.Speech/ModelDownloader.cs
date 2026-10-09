using System.Diagnostics;
using Helpers.Core.Speech;

namespace Helpers.Speech;

/// <summary>Fetches a sherpa-onnx model package once and unpacks it with the system's tar.</summary>
public static class ModelDownloader
{
    /// <summary>
    /// Makes sure <c>modelsRoot/packageName/model.onnx</c> exists, downloading and unpacking if not.
    /// The download is checked against <paramref name="sha256"/> before it is unpacked.
    /// </summary>
    public static async Task EnsureAsync(string modelsRoot, string packageName, string url, string? sha256, IProgress<EngineProgress>? progress, CancellationToken cancellationToken)
    {
        var modelDir = Path.Combine(modelsRoot, packageName);
        if (File.Exists(Path.Combine(modelDir, "model.onnx")))
        {
            return;
        }

        Directory.CreateDirectory(modelsRoot);
        var archive = Path.Combine(modelsRoot, packageName + ".tar.bz2");
        if (!File.Exists(archive))
        {
            await DownloadAsync(url, archive, sha256, progress, cancellationToken).ConfigureAwait(false);
        }

        progress?.Report(new EngineProgress("Unpacking the voice", null));
        await UnpackAsync(archive, modelsRoot, cancellationToken).ConfigureAwait(false);

        if (!File.Exists(Path.Combine(modelDir, "model.onnx")))
        {
            throw new InvalidOperationException("The voice package unpacked but model.onnx is missing.");
        }

        File.Delete(archive);
    }

    private static async Task DownloadAsync(string url, string destination, string? sha256, IProgress<EngineProgress>? progress, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1;
        var partial = destination + ".part";
        progress?.Report(new EngineProgress("Downloading the voice", total > 0 ? 0 : null));

        using var hasher = System.Security.Cryptography.SHA256.Create();
        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var file = File.Create(partial))
        {
            var chunk = new byte[1 << 16];
            long done = 0;
            var lastReported = -1;
            int read;
            while ((read = await source.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await file.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                hasher.TransformBlock(chunk, 0, read, null, 0);
                done += read;
                if (total > 0)
                {
                    var percent = (int)(done * 100 / total);
                    if (percent != lastReported)
                    {
                        progress?.Report(new EngineProgress("Downloading the voice", percent / 100.0));
                        lastReported = percent;
                    }
                }
            }
        }

        hasher.TransformFinalBlock([], 0, 0);
        if (sha256 is not null)
        {
            var actual = Convert.ToHexString(hasher.Hash!).ToLowerInvariant();
            if (!string.Equals(actual, sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(partial);
                throw new InvalidDataException("The voice download didn't match its checksum, so it was discarded.");
            }
        }

        File.Move(partial, destination, overwrite: true);
    }

    private static async Task UnpackAsync(string archive, string destination, CancellationToken cancellationToken)
    {
        var tar = OperatingSystem.IsWindows() ? Path.Combine(Environment.SystemDirectory, "tar.exe") : "tar";
        if (OperatingSystem.IsWindows() && !File.Exists(tar))
        {
            tar = "tar";
        }

        var info = new ProcessStartInfo(tar, $"-xf \"{archive}\" -C \"{destination}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(info) ?? throw new InvalidOperationException("tar could not be started.");
        var errors = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"tar failed (exit {process.ExitCode}): {errors}");
        }
    }
}
