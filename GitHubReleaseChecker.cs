using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text.Json;

namespace RemoSystemProfiler;

internal sealed record DownloadProgressInfo(
    double Percent,
    long DownloadedBytes,
    long TotalBytes,
    double BytesPerSecond);

internal sealed record GitHubReleaseAssetInfo(
    string Name,
    string DownloadUrl,
    string ContentType,
    long Size);

internal sealed record GitHubReleaseInfo(
    string Version,
    string Title,
    string Notes,
    string Url,
    DateTimeOffset? PublishedAt,
    GitHubReleaseAssetInfo? DownloadAsset);

internal sealed record GitHubReleaseCheckResult(
    bool IsSuccess,
    bool HasRelease,
    bool IsUpdateAvailable,
    GitHubReleaseInfo? Release,
    string? ErrorMessage)
{
    public static GitHubReleaseCheckResult Success(bool isUpdateAvailable, GitHubReleaseInfo release) =>
        new(true, true, isUpdateAvailable, release, null);

    public static GitHubReleaseCheckResult NoRelease() =>
        new(true, false, false, null, null);

    public static GitHubReleaseCheckResult Failed(string message) =>
        new(false, false, false, null, message);
}

internal static class GitHubReleaseChecker
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/Remyuu/RemoSystemProfiler/releases/latest";
    private static readonly Uri LatestReleaseUri = new(LatestReleaseUrl);

    private static readonly HttpClient Client = CreateClient();

    public static async Task<GitHubReleaseCheckResult> CheckLatestAsync(string currentVersion, CancellationToken token)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, LatestReleaseUri);
            using HttpResponseMessage response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return GitHubReleaseCheckResult.NoRelease();
            }

            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: token).ConfigureAwait(false);
            JsonElement root = document.RootElement;
            string version = ReadString(root, "tag_name");
            string title = ReadString(root, "name");
            string notes = ReadString(root, "body");
            string url = ReadString(root, "html_url");
            DateTimeOffset? publishedAt = ReadDateTimeOffset(root, "published_at");
            GitHubReleaseAssetInfo? downloadAsset = ChooseDownloadAsset(ReadAssets(root));

            if (string.IsNullOrWhiteSpace(version))
            {
                return GitHubReleaseCheckResult.Failed("Latest release does not include a version tag.");
            }

            GitHubReleaseInfo release = new(
                version,
                string.IsNullOrWhiteSpace(title) ? version : title,
                string.IsNullOrWhiteSpace(notes) ? Localization.UpdateNoReleaseNotes : notes.Trim(),
                string.IsNullOrWhiteSpace(url) ? "https://github.com/Remyuu/RemoSystemProfiler/releases" : url,
                publishedAt,
                downloadAsset);

            return GitHubReleaseCheckResult.Success(IsNewerVersion(currentVersion, version), release);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return GitHubReleaseCheckResult.Failed(Localization.UpdateCheckCanceled);
        }
        catch (Exception ex)
        {
            return GitHubReleaseCheckResult.Failed(ex.Message);
        }
    }

    public static async Task DownloadAssetAsync(
        GitHubReleaseAssetInfo asset,
        string destinationPath,
        IProgress<DownloadProgressInfo>? progress,
        CancellationToken token)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, asset.DownloadUrl);
        using HttpResponseMessage response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? ".");
        await using Stream source = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        await using FileStream destination = File.Create(destinationPath);

        long totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
        long downloadedBytes = 0;
        byte[] buffer = new byte[128 * 1024];
        Stopwatch stopwatch = Stopwatch.StartNew();
        progress?.Report(new DownloadProgressInfo(0, 0, totalBytes, 0));

        while (true)
        {
            int read = await source.ReadAsync(buffer, token).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
            downloadedBytes += read;
            double elapsedSeconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
            double bytesPerSecond = downloadedBytes / elapsedSeconds;
            if (totalBytes > 0)
            {
                double percent = Math.Clamp(downloadedBytes * 100d / totalBytes, 0, 100);
                progress?.Report(new DownloadProgressInfo(percent, downloadedBytes, totalBytes, bytesPerSecond));
            }
            else
            {
                progress?.Report(new DownloadProgressInfo(0, downloadedBytes, totalBytes, bytesPerSecond));
            }
        }

        double finalSpeed = stopwatch.Elapsed.TotalSeconds > 0 ? downloadedBytes / stopwatch.Elapsed.TotalSeconds : 0;
        progress?.Report(new DownloadProgressInfo(100, downloadedBytes, totalBytes, finalSpeed));
    }

    private static HttpClient CreateClient()
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(12)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RemoSystemProfiler", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement element, string propertyName)
    {
        string text = ReadString(element, propertyName);
        return DateTimeOffset.TryParse(text, out DateTimeOffset value) ? value : null;
    }

    private static IReadOnlyList<GitHubReleaseAssetInfo> ReadAssets(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out JsonElement assetsElement) || assetsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<GitHubReleaseAssetInfo> assets = [];
        foreach (JsonElement assetElement in assetsElement.EnumerateArray())
        {
            string name = ReadString(assetElement, "name");
            string downloadUrl = ReadString(assetElement, "browser_download_url");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(downloadUrl))
            {
                continue;
            }

            string contentType = ReadString(assetElement, "content_type");
            long size = assetElement.TryGetProperty("size", out JsonElement sizeElement) && sizeElement.TryGetInt64(out long value)
                ? value
                : 0;
            assets.Add(new GitHubReleaseAssetInfo(name, downloadUrl, contentType, size));
        }

        return assets;
    }

    private static GitHubReleaseAssetInfo? ChooseDownloadAsset(IReadOnlyList<GitHubReleaseAssetInfo> assets)
    {
        return assets
            .OrderBy(asset => AssetRank(asset.Name))
            .FirstOrDefault(asset => AssetRank(asset.Name) < 100);
    }

    private static int AssetRank(string name)
    {
        string lower = name.ToLowerInvariant();
        if (lower.EndsWith(".zip") && lower.Contains("remosystemprofiler"))
        {
            return 0;
        }

        if (lower.EndsWith(".zip"))
        {
            return 1;
        }

        if (lower.EndsWith(".msi"))
        {
            return 2;
        }

        if (lower.EndsWith(".exe"))
        {
            return 3;
        }

        return 100;
    }

    private static bool IsNewerVersion(string currentVersion, string releaseVersion)
    {
        int[] current = ParseVersionParts(currentVersion);
        int[] latest = ParseVersionParts(releaseVersion);
        int count = Math.Max(current.Length, latest.Length);
        for (int i = 0; i < count; i++)
        {
            int currentPart = i < current.Length ? current[i] : 0;
            int latestPart = i < latest.Length ? latest[i] : 0;
            if (latestPart != currentPart)
            {
                return latestPart > currentPart;
            }
        }

        return false;
    }

    private static int[] ParseVersionParts(string version)
    {
        string normalized = version.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        int suffixIndex = normalized.IndexOfAny(['-', '+']);
        if (suffixIndex >= 0)
        {
            normalized = normalized[..suffixIndex];
        }

        return normalized
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out int value) ? value : 0)
            .ToArray();
    }
}
