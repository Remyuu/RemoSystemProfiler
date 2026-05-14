using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RemoSystemProfiler.Core;

public sealed class BenchmarkUploadDto
{
    [JsonPropertyName("app_version")]
    public string AppVersion { get; set; } = string.Empty;

    [JsonPropertyName("benchmark_version")]
    public string BenchmarkVersion { get; set; } = string.Empty;

    [JsonPropertyName("profile")]
    public string Profile { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = BenchmarkPayload.DefaultDisplayName;

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("cpu_core_score")]
    public double? CpuCoreScore { get; set; }

    [JsonPropertyName("cpu_mixed_score")]
    public double? CpuMixedScore { get; set; }

    [JsonPropertyName("scimark_score")]
    public double? SciMarkScore { get; set; }

    [JsonPropertyName("zstd_compress_gbps")]
    public double? ZstdCompressGbps { get; set; }

    [JsonPropertyName("zstd_decompress_gbps")]
    public double? ZstdDecompressGbps { get; set; }

    [JsonPropertyName("zstd_ratio")]
    public double? ZstdRatio { get; set; }

    [JsonPropertyName("xxhash3_gbps")]
    public double? XxHash3Gbps { get; set; }

    [JsonPropertyName("cpu_name")]
    public string? CpuName { get; set; }

    [JsonPropertyName("cpu_cores")]
    public int? CpuCores { get; set; }

    [JsonPropertyName("cpu_threads")]
    public int? CpuThreads { get; set; }

    [JsonPropertyName("avg_frequency_ghz")]
    public double? AvgFrequencyGhz { get; set; }

    [JsonPropertyName("max_temperature_c")]
    public double? MaxTemperatureC { get; set; }

    [JsonPropertyName("power_thermal_status")]
    public string? PowerThermalStatus { get; set; }

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; set; } = BenchmarkPayload.ValidationOk;

    [JsonPropertyName("checksum")]
    public string Checksum { get; set; } = string.Empty;

    [JsonPropertyName("installation_id")]
    public string InstallationId { get; set; } = string.Empty;

    [JsonPropertyName("client_created_at")]
    public string ClientCreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("telemetry_samples")]
    public IReadOnlyList<BenchmarkTelemetrySample>? TelemetrySamples { get; set; }
}

public sealed class BenchmarkLeaderboardQuery
{
    public string BenchmarkVersion { get; init; } = BenchmarkRunner.Version;

    public string AppVersion { get; init; } = string.Empty;

    public string Profile { get; init; } = "standard";

    public string Mode { get; init; } = "multi";

    public BenchmarkScoreKind ScoreKind { get; init; } = BenchmarkScoreKind.CpuCore;

    public int Limit { get; init; } = 50;
}

public sealed class BenchmarkLeaderboardEntry
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("app_version")]
    public string AppVersion { get; init; } = string.Empty;

    [JsonPropertyName("benchmark_version")]
    public string BenchmarkVersion { get; init; } = string.Empty;

    [JsonPropertyName("profile")]
    public string Profile { get; init; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; init; } = string.Empty;

    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("cpu_core_score")]
    public double? CpuCoreScore { get; init; }

    [JsonPropertyName("cpu_mixed_score")]
    public double? CpuMixedScore { get; init; }

    [JsonPropertyName("ranking_score")]
    public double? RankingScore { get; init; }

    [JsonPropertyName("scimark_score")]
    public double? SciMarkScore { get; init; }

    [JsonPropertyName("zstd_compress_gbps")]
    public double? ZstdCompressGbps { get; init; }

    [JsonPropertyName("zstd_decompress_gbps")]
    public double? ZstdDecompressGbps { get; init; }

    [JsonPropertyName("zstd_ratio")]
    public double? ZstdRatio { get; init; }

    [JsonPropertyName("xxhash3_gbps")]
    public double? XxHash3Gbps { get; init; }

    [JsonPropertyName("cpu_name")]
    public string CpuName { get; init; } = string.Empty;

    [JsonPropertyName("cpu_cores")]
    public int? CpuCores { get; init; }

    [JsonPropertyName("cpu_threads")]
    public int? CpuThreads { get; init; }

    [JsonPropertyName("avg_frequency_ghz")]
    public double? AvgFrequencyGhz { get; init; }

    [JsonPropertyName("max_temperature_c")]
    public double? MaxTemperatureC { get; init; }

    [JsonPropertyName("power_thermal_status")]
    public string? PowerThermalStatus { get; init; }

    [JsonPropertyName("client_created_at")]
    public string? ClientCreatedAt { get; init; }

    [JsonPropertyName("server_created_at")]
    public string? ServerCreatedAt { get; init; }

    [JsonPropertyName("telemetry_samples")]
    public IReadOnlyList<BenchmarkTelemetrySample>? TelemetrySamples { get; init; }
}

public sealed class BenchmarkTelemetrySample
{
    [JsonPropertyName("elapsed_s")]
    public double ElapsedSeconds { get; init; }

    [JsonPropertyName("cpu_load_percent")]
    public double? CpuLoadPercent { get; init; }

    [JsonPropertyName("cpu_max_temperature_c")]
    public double? CpuMaxTemperatureC { get; init; }

    [JsonPropertyName("cpu_package_power_w")]
    public double? CpuPackagePowerW { get; init; }

    [JsonPropertyName("cpu_clock_ghz")]
    public double? CpuClockGhz { get; init; }
}

public enum BenchmarkUploadStatus
{
    Uploaded,
    Duplicate,
    RateLimited,
    Invalid,
    Failed
}

public sealed record BenchmarkUploadResult(
    BenchmarkUploadStatus Status,
    string Message,
    HttpStatusCode? HttpStatusCode = null);

public sealed class BenchmarkApiClient : IDisposable
{
    private static readonly Uri ResultsEndpoint = new("https://remoooo.com/wp-json/remo-benchmark/v1/results");
    private static readonly Uri LeaderboardEndpoint = new("https://remoooo.com/wp-json/remo-benchmark/v1/leaderboard");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    public BenchmarkApiClient()
        : this(new HttpClient(), ownsHttpClient: true)
    {
    }

    public BenchmarkApiClient(HttpClient http)
        : this(http, ownsHttpClient: false)
    {
    }

    private BenchmarkApiClient(HttpClient http, bool ownsHttpClient)
    {
        _http = http;
        _ownsHttpClient = ownsHttpClient;
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<BenchmarkUploadResult> UploadAsync(BenchmarkUploadDto dto, CancellationToken cancellationToken)
    {
        if (!BenchmarkPayload.TryValidateForUpload(dto, out string validationError))
        {
            return new BenchmarkUploadResult(BenchmarkUploadStatus.Invalid, validationError);
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await _http.PostAsJsonAsync(ResultsEndpoint, dto, JsonOptions, cancellationToken).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if ((int)response.StatusCode == 201)
                {
                    return new BenchmarkUploadResult(BenchmarkUploadStatus.Uploaded, body, response.StatusCode);
                }

                if (response.IsSuccessStatusCode && body.Contains("\"duplicate\"", StringComparison.OrdinalIgnoreCase))
                {
                    return new BenchmarkUploadResult(BenchmarkUploadStatus.Duplicate, body, response.StatusCode);
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    return new BenchmarkUploadResult(BenchmarkUploadStatus.RateLimited, body, response.StatusCode);
                }

                if ((int)response.StatusCode >= 500 && attempt < 2)
                {
                    await Task.Delay(RetryDelay(attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return new BenchmarkUploadResult(BenchmarkUploadStatus.Failed, body, response.StatusCode);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsTransientUploadException(ex) && attempt < 2)
            {
                await Task.Delay(RetryDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new BenchmarkUploadResult(BenchmarkUploadStatus.Failed, ex.Message);
            }
        }

        return new BenchmarkUploadResult(BenchmarkUploadStatus.Failed, "Upload retry limit reached.");
    }

    public async Task<IReadOnlyList<BenchmarkLeaderboardEntry>> GetLeaderboardAsync(BenchmarkLeaderboardQuery query, CancellationToken cancellationToken)
    {
        Uri requestUri = BuildLeaderboardUri(query);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await _http.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode >= 500 && attempt < 2)
                {
                    await Task.Delay(RetryDelay(attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<List<BenchmarkLeaderboardEntry>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
                    ?? [];
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception) when (attempt < 2)
            {
                await Task.Delay(RetryDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        return [];
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }

    private static TimeSpan RetryDelay(int attempt) => TimeSpan.FromMilliseconds(attempt == 0 ? 500 : 1500);

    private static bool IsTransientUploadException(Exception ex)
    {
        return ex is HttpRequestException or TaskCanceledException or IOException;
    }

    private static Uri BuildLeaderboardUri(BenchmarkLeaderboardQuery query)
    {
        StringBuilder builder = new();
        AppendQuery(builder, "benchmark_version", query.BenchmarkVersion);
        AppendQuery(builder, "app_version", query.AppVersion);
        AppendQuery(builder, "profile", query.Profile);
        AppendQuery(builder, "mode", query.Mode);
        AppendQuery(builder, "ranking", BenchmarkPayload.ScoreKindToApiValue(query.ScoreKind));
        AppendQuery(builder, "limit", Math.Clamp(query.Limit, 1, 100).ToString(CultureInfo.InvariantCulture));
        return new Uri($"{LeaderboardEndpoint}?{builder}");
    }

    private static void AppendQuery(StringBuilder builder, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('&');
        }

        builder
            .Append(Uri.EscapeDataString(name))
            .Append('=')
            .Append(Uri.EscapeDataString(value));
    }
}

public static class BenchmarkIdentityStore
{
    private const string ApplicationFolderName = "RemoSystemProfiler";
    private const string InstallationIdFileName = "installation_id.txt";

    public static string GetOrCreateInstallationId()
    {
        string path = ResolveInstallationIdPath();
        try
        {
            if (File.Exists(path))
            {
                string existing = File.ReadAllText(path).Trim();
                if (Guid.TryParse(existing, out Guid parsed))
                {
                    return parsed.ToString("D");
                }
            }

            string generated = Guid.NewGuid().ToString("D");
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppContext.BaseDirectory);
            File.WriteAllText(path, generated);
            return generated;
        }
        catch (IOException)
        {
            return Guid.NewGuid().ToString("D");
        }
        catch (UnauthorizedAccessException)
        {
            return Guid.NewGuid().ToString("D");
        }
    }

    private static string ResolveInstallationIdPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string root = string.IsNullOrWhiteSpace(appData)
            ? AppContext.BaseDirectory
            : Path.Combine(appData, ApplicationFolderName);
        return Path.Combine(root, InstallationIdFileName);
    }
}

public static class BenchmarkPayload
{
    public const string ValidationOk = "OK";
    public const string DefaultDisplayName = "Anonymous";
    private const int MaxDisplayNameLength = 40;
    private const int MaxTelemetrySamples = 240;
    private const double MaxAcceptedScore = 100_000_000d;
    private static readonly JsonWriterOptions CanonicalWriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false
    };

    public static string ProfileToApiValue(BenchmarkRunProfile profile) => profile switch
    {
        BenchmarkRunProfile.Quick => "quick",
        BenchmarkRunProfile.Sustained => "sustained",
        _ => "standard"
    };

    public static string ModeToApiValue(int workerCount) => workerCount == 1 ? "single" : "multi";

    public static string ScoreKindToApiValue(BenchmarkScoreKind scoreKind) => scoreKind switch
    {
        BenchmarkScoreKind.CpuMixed => "cpu_mixed",
        _ => "cpu_core"
    };

    public static string UtcTimestamp(DateTimeOffset timestamp) => timestamp.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static double RoundMetric(double value, int digits = 6)
    {
        return Math.Round(value, digits, MidpointRounding.AwayFromZero);
    }

    public static string NormalizeDisplayName(string? displayName)
    {
        string normalized = string.IsNullOrWhiteSpace(displayName)
            ? DefaultDisplayName
            : displayName.Trim();
        normalized = string.Join(" ", normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= MaxDisplayNameLength
            ? normalized
            : normalized[..MaxDisplayNameLength];
    }

    public static void NormalizeMetrics(BenchmarkUploadDto dto)
    {
        dto.DisplayName = NormalizeDisplayName(dto.DisplayName);
        dto.CpuMixedScore ??= dto.Score;
        dto.Score = RoundMetric(dto.Score, 2);
        dto.CpuCoreScore = RoundNullable(dto.CpuCoreScore, 2);
        dto.CpuMixedScore = RoundNullable(dto.CpuMixedScore, 2);
        dto.SciMarkScore = RoundNullable(dto.SciMarkScore, 2);
        dto.ZstdCompressGbps = RoundNullable(dto.ZstdCompressGbps);
        dto.ZstdDecompressGbps = RoundNullable(dto.ZstdDecompressGbps);
        dto.ZstdRatio = BenchmarkRunner.IsLegacyVersion(dto.BenchmarkVersion)
            ? RoundNullable(dto.ZstdRatio)
            : null;
        dto.XxHash3Gbps = RoundNullable(dto.XxHash3Gbps);
        dto.AvgFrequencyGhz = RoundNullable(dto.AvgFrequencyGhz, 3);
        dto.MaxTemperatureC = RoundNullable(dto.MaxTemperatureC, 1);
        dto.TelemetrySamples = NormalizeTelemetrySamples(dto.TelemetrySamples);
    }

    public static string ComputeChecksum(BenchmarkUploadDto dto)
    {
        string canonical = BuildCanonicalJson(dto);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool TryValidateForUpload(BenchmarkUploadDto dto, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(dto.AppVersion)
            || string.IsNullOrWhiteSpace(dto.BenchmarkVersion)
            || string.IsNullOrWhiteSpace(dto.Profile)
            || string.IsNullOrWhiteSpace(dto.Mode))
        {
            error = "Missing benchmark identity fields.";
            return false;
        }

        if (!BenchmarkRunner.IsSupportedVersion(dto.BenchmarkVersion))
        {
            error = "Unsupported benchmark version.";
            return false;
        }

        if (dto.Profile is not ("quick" or "standard" or "sustained"))
        {
            error = "Invalid benchmark profile.";
            return false;
        }

        if (dto.Mode is not ("single" or "multi"))
        {
            error = "Invalid benchmark mode.";
            return false;
        }

        if (!IsAcceptedScore(dto.Score))
        {
            error = "Invalid benchmark score.";
            return false;
        }

        if (dto.CpuMixedScore is not null && !IsAcceptedScore(dto.CpuMixedScore.Value))
        {
            error = "Invalid CPU mixed score.";
            return false;
        }

        if (BenchmarkRunner.SupportsCpuCoreScore(dto.BenchmarkVersion)
            && (dto.CpuCoreScore is null || !IsAcceptedScore(dto.CpuCoreScore.Value)))
        {
            error = "Invalid CPU core score.";
            return false;
        }

        if (!string.Equals(dto.ValidationStatus, ValidationOk, StringComparison.Ordinal))
        {
            error = "Benchmark validation did not pass.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.InstallationId) || string.IsNullOrWhiteSpace(dto.Checksum))
        {
            error = "Missing installation id or checksum.";
            return false;
        }

        if (!Guid.TryParse(dto.InstallationId, out _))
        {
            error = "Invalid installation id.";
            return false;
        }

        if (dto.Checksum.Length != 64 || dto.Checksum.Any(ch => !Uri.IsHexDigit(ch)))
        {
            error = "Invalid checksum.";
            return false;
        }

        if (!AllNullableMetricsAreValid(dto))
        {
            error = "Invalid benchmark metric.";
            return false;
        }

        if (!TelemetrySamplesAreValid(dto.TelemetrySamples))
        {
            error = "Invalid benchmark telemetry.";
            return false;
        }

        return true;
    }

    private static string BuildCanonicalJson(BenchmarkUploadDto dto)
    {
        List<CanonicalProperty> properties = [];
        Add(properties, "app_version", dto.AppVersion);
        Add(properties, "benchmark_version", dto.BenchmarkVersion);
        Add(properties, "client_created_at", dto.ClientCreatedAt);
        Add(properties, "cpu_cores", dto.CpuCores);
        Add(properties, "cpu_core_score", dto.CpuCoreScore);
        Add(properties, "cpu_mixed_score", dto.CpuMixedScore);
        Add(properties, "cpu_name", dto.CpuName);
        Add(properties, "cpu_threads", dto.CpuThreads);
        Add(properties, "installation_id", dto.InstallationId);
        Add(properties, "mode", dto.Mode);
        Add(properties, "profile", dto.Profile);
        Add(properties, "score", dto.Score);
        Add(properties, "scimark_score", dto.SciMarkScore);
        Add(properties, "xxhash3_gbps", dto.XxHash3Gbps);
        Add(properties, "zstd_compress_gbps", dto.ZstdCompressGbps);
        Add(properties, "zstd_decompress_gbps", dto.ZstdDecompressGbps);
        if (BenchmarkRunner.IsLegacyVersion(dto.BenchmarkVersion))
        {
            Add(properties, "zstd_ratio", dto.ZstdRatio);
        }

        properties.Sort((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, CanonicalWriterOptions);
        writer.WriteStartObject();
        foreach (CanonicalProperty property in properties)
        {
            writer.WritePropertyName(property.Name);
            WriteCanonicalValue(writer, property.Value);
        }

        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Add(List<CanonicalProperty> properties, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            properties.Add(new CanonicalProperty(name, value));
        }
    }

    private static void Add(List<CanonicalProperty> properties, string name, int? value)
    {
        if (value is { } actual)
        {
            properties.Add(new CanonicalProperty(name, actual));
        }
    }

    private static void Add(List<CanonicalProperty> properties, string name, double? value)
    {
        if (value is { } actual && double.IsFinite(actual))
        {
            properties.Add(new CanonicalProperty(name, actual));
        }
    }

    private static void WriteCanonicalValue(Utf8JsonWriter writer, object value)
    {
        switch (value)
        {
            case string text:
                writer.WriteStringValue(text);
                break;
            case int number:
                writer.WriteNumberValue(number);
                break;
            case double number:
                writer.WriteNumberValue(number == 0 ? 0 : number);
                break;
            default:
                throw new InvalidOperationException($"Unsupported canonical value type: {value.GetType().Name}");
        }
    }

    private static double? RoundNullable(double? value, int digits = 6)
    {
        return value is { } actual && double.IsFinite(actual)
            ? RoundMetric(actual, digits)
            : null;
    }

    private static IReadOnlyList<BenchmarkTelemetrySample>? NormalizeTelemetrySamples(IReadOnlyList<BenchmarkTelemetrySample>? samples)
    {
        if (samples is null || samples.Count == 0)
        {
            return null;
        }

        List<BenchmarkTelemetrySample> normalized = new(Math.Min(samples.Count, MaxTelemetrySamples));
        int stride = Math.Max(1, (int)Math.Ceiling(samples.Count / (double)MaxTelemetrySamples));
        for (int i = 0; i < samples.Count; i += stride)
        {
            BenchmarkTelemetrySample sample = samples[i];
            if (!double.IsFinite(sample.ElapsedSeconds) || sample.ElapsedSeconds < 0)
            {
                continue;
            }

            normalized.Add(new BenchmarkTelemetrySample
            {
                ElapsedSeconds = RoundMetric(sample.ElapsedSeconds, 3),
                CpuLoadPercent = RoundNullable(sample.CpuLoadPercent, 2),
                CpuMaxTemperatureC = RoundNullable(sample.CpuMaxTemperatureC, 2),
                CpuPackagePowerW = RoundNullable(sample.CpuPackagePowerW, 3),
                CpuClockGhz = RoundNullable(sample.CpuClockGhz, 3)
            });
        }

        return normalized.Count == 0 ? null : normalized;
    }

    private static bool IsAcceptedScore(double score)
    {
        return double.IsFinite(score) && score > 0 && score <= MaxAcceptedScore;
    }

    private static bool IsAcceptedMetric(double? value)
    {
        return value is null || (double.IsFinite(value.Value) && value.Value >= 0);
    }

    private static bool IsAcceptedPercent(double? value)
    {
        return value is null || (double.IsFinite(value.Value) && value.Value is >= 0 and <= 100);
    }

    private static bool IsAcceptedTelemetryValue(double? value, double max)
    {
        return value is null || (double.IsFinite(value.Value) && value.Value >= 0 && value.Value <= max);
    }

    private static bool TelemetrySamplesAreValid(IReadOnlyList<BenchmarkTelemetrySample>? samples)
    {
        if (samples is null)
        {
            return true;
        }

        if (samples.Count > MaxTelemetrySamples)
        {
            return false;
        }

        double previousElapsed = -1;
        foreach (BenchmarkTelemetrySample sample in samples)
        {
            if (!double.IsFinite(sample.ElapsedSeconds) || sample.ElapsedSeconds < 0 || sample.ElapsedSeconds < previousElapsed)
            {
                return false;
            }

            if (!IsAcceptedPercent(sample.CpuLoadPercent)
                || !IsAcceptedTelemetryValue(sample.CpuMaxTemperatureC, 130)
                || !IsAcceptedTelemetryValue(sample.CpuPackagePowerW, 1000)
                || !IsAcceptedTelemetryValue(sample.CpuClockGhz, 10))
            {
                return false;
            }

            previousElapsed = sample.ElapsedSeconds;
        }

        return true;
    }

    private static bool AllNullableMetricsAreValid(BenchmarkUploadDto dto)
    {
        return IsAcceptedMetric(dto.CpuCoreScore)
            && IsAcceptedMetric(dto.CpuMixedScore)
            && IsAcceptedMetric(dto.SciMarkScore)
            && IsAcceptedMetric(dto.ZstdCompressGbps)
            && IsAcceptedMetric(dto.ZstdDecompressGbps)
            && IsAcceptedMetric(dto.ZstdRatio)
            && IsAcceptedMetric(dto.XxHash3Gbps)
            && IsAcceptedMetric(dto.AvgFrequencyGhz)
            && IsAcceptedMetric(dto.MaxTemperatureC);
    }

    private readonly record struct CanonicalProperty(string Name, object Value);
}
