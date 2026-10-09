using System.Text.Json;

namespace Helpers.Core.Updates;

/// <summary>A downloadable file from a release, with the checksum the app checks before trusting it.</summary>
public sealed record ReleaseFile(string Url, string Sha256);

/// <summary>What the latest release says about itself, from the version.json the release workflow attaches.</summary>
public sealed record ReleaseInfo(string Version, string Notes, string Page, ReleaseFile? Installer, ReleaseFile? Zip);

/// <summary>
/// The update check, opt-in. One request a day to the latest GitHub release
/// for its version file; nothing is sent but the request itself. Pure
/// functions here; the fetching and the toast live in the app.
/// </summary>
public static class UpdateCheck
{
    public const string VersionUrl = "https://github.com/drhumphrey/helpers/releases/latest/download/version.json";

    public const string ReleasesPage = "https://github.com/drhumphrey/helpers/releases";

    /// <summary>A little under a day, so a daily launch at the same time still counts.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromHours(23);

    public static bool IsDue(DateTime? lastCheckedUtc, DateTime nowUtc) =>
        lastCheckedUtc is null || nowUtc - lastCheckedUtc.Value >= Interval;

    /// <summary>Reads the version file. Null for anything that isn't one.</summary>
    public static ReleaseInfo? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var version = GetString(root, "version");
            if (string.IsNullOrWhiteSpace(version) || ParseVersion(version) is null)
            {
                return null;
            }

            return new ReleaseInfo(
                version.Trim(),
                GetString(root, "notes") ?? string.Empty,
                GetString(root, "page") ?? ReleasesPage,
                GetFile(root, "installer"),
                GetFile(root, "zip"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>True when the candidate is a later version than the one running. "v" prefixes and build suffixes are ignored.</summary>
    public static bool IsNewer(string candidate, string current)
    {
        var a = ParseVersion(candidate);
        var b = ParseVersion(current);
        return a is not null && b is not null && a > b;
    }

    public static Version? ParseVersion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var core = text.Trim().TrimStart('v', 'V');
        var cut = core.IndexOfAny(['-', '+', ' ']);
        if (cut >= 0)
        {
            core = core[..cut];
        }

        var parts = core.Split('.');
        if (parts.Length is < 2 or > 4 || parts.Any(p => !int.TryParse(p, out var n) || n < 0))
        {
            return null;
        }

        return Version.TryParse(parts.Length == 2 ? core + ".0" : core, out var version) ? version : null;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static ReleaseFile? GetFile(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var file) || file.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var url = GetString(file, "url");
        var sha = GetString(file, "sha256");
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(sha) || sha.Length != 64)
        {
            return null;
        }

        return new ReleaseFile(url, sha.ToLowerInvariant());
    }
}

/// <summary>The update check's settings. Off until the user says yes.</summary>
public sealed class UpdateSettings
{
    /// <summary>Opt-in: the first-run screen asks, Settings has the switch.</summary>
    public bool CheckForUpdates { get; set; }

    /// <summary>True once the first-run screen has put the question.</summary>
    public bool Asked { get; set; }

    public DateTime? LastCheckedUtc { get; set; }

    /// <summary>A version the user chose to skip. Later versions are still offered.</summary>
    public string? SkippedVersion { get; set; }
}
