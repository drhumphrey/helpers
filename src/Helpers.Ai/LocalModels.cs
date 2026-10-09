using System.Security.Cryptography;

namespace Helpers.Ai;

/// <summary>One downloadable local model: where it comes from, how big it is, and its checksum.</summary>
public sealed record LocalModel(string Name, string FileName, string Url, string Sha256, long Size, string Description)
{
    public string SizeText => $"{Size / 1_073_741_824.0:0.0} GB";
}

/// <summary>The local models the app knows how to fetch. Qwen3 instruct, GGUF, from Qwen's own Hugging Face repos.</summary>
public static class LocalModels
{
    public static readonly LocalModel Qwen3_4B = new(
        "Qwen3-4B-Q4_K_M",
        "Qwen3-4B-Q4_K_M.gguf",
        "https://huggingface.co/Qwen/Qwen3-4B-GGUF/resolve/main/Qwen3-4B-Q4_K_M.gguf",
        "7485fe6f11af29433bc51cab58009521f205840f5b4ae3a32fa7f92e8534fdf5",
        2_497_280_256,
        "Qwen3 4B. The default. About 3 GB of memory while loaded.");

    public static readonly LocalModel Qwen3_1_7B = new(
        "Qwen3-1.7B-Q8_0",
        "Qwen3-1.7B-Q8_0.gguf",
        "https://huggingface.co/Qwen/Qwen3-1.7B-GGUF/resolve/main/Qwen3-1.7B-Q8_0.gguf",
        "061b54daade076b5d3362dac252678d17da8c68f07560be70818cace6590cb1a",
        1_834_426_016,
        "Qwen3 1.7B. Smaller and faster, for PCs with less memory. Rougher notes.");

    public static readonly LocalModel[] All = [Qwen3_4B, Qwen3_1_7B];

    public static LocalModel ByName(string? name) => All.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)) ?? Qwen3_4B;

    public static string PathFor(LocalModel model, string folder) => Path.Combine(folder, model.FileName);

    public static bool IsDownloaded(LocalModel model, string folder)
    {
        var path = PathFor(model, folder);
        return File.Exists(path) && new FileInfo(path).Length == model.Size;
    }

    /// <summary>
    /// Downloads to a temporary file with progress, checks the SHA-256, then
    /// moves it into place. A cancelled or failed download leaves nothing behind.
    /// </summary>
    public static async Task DownloadAsync(LocalModel model, string folder, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folder);
        var path = PathFor(model, folder);
        var temp = path + ".part";

        // One download of a file at a time, and none at all if it is already here and whole.
        await DownloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsDownloaded(model, folder))
            {
                progress?.Report(1);
                return;
            }

            await FetchAsync(model, path, temp, progress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            DownloadGate.Release();
        }
    }

    private static readonly SemaphoreSlim DownloadGate = new(1, 1);

    private static async Task FetchAsync(LocalModel model, string path, string temp, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Helpers/0.1 (+https://github.com/drhumphrey/helpers)");

        try
        {
            using var response = await http.GetAsync(model.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? model.Size;

            using var sha = SHA256.Create();
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var target = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true))
            {
                var buffer = new byte[1 << 20];
                long done = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    done += read;
                    progress?.Report(total > 0 ? (double)done / total : 0);
                }

                sha.TransformFinalBlock([], 0, 0);
            }

            var actual = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
            if (!string.Equals(actual, model.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The downloaded model file didn't match its checksum, so it was discarded.");
            }

            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(temp);
            }
            catch (IOException)
            {
            }

            throw;
        }
    }
}
