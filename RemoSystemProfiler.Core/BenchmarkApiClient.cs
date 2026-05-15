using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RemoSystemProfiler.Core;

public sealed class BenchmarkUploadDto
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = BenchmarkPayload.SchemaVersion;

    [JsonPropertyName("suite_id")]
    public string SuiteId { get; set; } = BenchmarkPayload.CpuSuiteId;

    [JsonPropertyName("suite_version")]
    public string SuiteVersion { get; set; } = BenchmarkRunner.Version;

    [JsonPropertyName("app_version")]
    public string AppVersion { get; set; } = string.Empty;

    [JsonPropertyName("profile")]
    public string Profile { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = BenchmarkPayload.DefaultDisplayName;

    [JsonPropertyName("client_created_at")]
    public string ClientCreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("hardware")]
    public BenchmarkHardwareInfo? Hardware { get; set; }

    [JsonPropertyName("scores")]
    public IReadOnlyList<BenchmarkValueDto> Scores { get; set; } = [];

    [JsonPropertyName("metrics")]
    public IReadOnlyList<BenchmarkValueDto> Metrics { get; set; } = [];

    [JsonPropertyName("telemetry_manifest")]
    public BenchmarkTelemetryManifest? TelemetryManifest { get; set; }

    [JsonIgnore]
    public IReadOnlyList<BenchmarkTelemetrySample>? LocalTelemetrySamples { get; set; }
}

public sealed class BenchmarkValueDto
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; init; }

    [JsonPropertyName("unit")]
    public string? Unit { get; init; }
}

public sealed class BenchmarkHardwareInfo
{
    [JsonPropertyName("cpu")]
    public BenchmarkCpuHardwareInfo? Cpu { get; init; }
}

public sealed class BenchmarkCpuHardwareInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("cores")]
    public int? Cores { get; init; }

    [JsonPropertyName("threads")]
    public int? Threads { get; init; }
}

public sealed class BenchmarkTelemetryManifest
{
    [JsonPropertyName("available")]
    public bool Available { get; init; }

    [JsonPropertyName("series")]
    public IReadOnlyList<string> Series { get; init; } = [];
}

public sealed class BenchmarkTelemetryChunkDto
{
    [JsonPropertyName("chunk_index")]
    public int ChunkIndex { get; init; }

    [JsonPropertyName("series")]
    public IReadOnlyDictionary<string, IReadOnlyList<double[]>> Series { get; init; } =
        new Dictionary<string, IReadOnlyList<double[]>>();
}

public sealed class BenchmarkLeaderboardQuery
{
    public string SuiteId { get; init; } = BenchmarkPayload.CpuSuiteId;

    public string SuiteVersion { get; init; } = BenchmarkRunner.Version;

    public string Profile { get; init; } = "standard";

    public string Mode { get; init; } = "multi";

    public BenchmarkScoreKind ScoreKind { get; init; } = BenchmarkScoreKind.CpuCore;

    public int Limit { get; init; } = 50;

    public string Cursor { get; init; } = string.Empty;
}

public sealed class BenchmarkLeaderboardResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<BenchmarkLeaderboardEntry> Items { get; init; } = [];

    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; init; }
}

public sealed class BenchmarkLeaderboardEntry
{
    [JsonPropertyName("run_id")]
    public string RunId { get; init; } = string.Empty;

    [JsonPropertyName("rank")]
    public int? Rank { get; init; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; init; } = string.Empty;

    [JsonPropertyName("ranking_key")]
    public string RankingKey { get; init; } = string.Empty;

    [JsonPropertyName("ranking_score")]
    public double? RankingScore { get; init; }

    [JsonPropertyName("app_version")]
    public string AppVersion { get; init; } = string.Empty;

    [JsonPropertyName("suite_id")]
    public string SuiteId { get; init; } = BenchmarkPayload.CpuSuiteId;

    [JsonPropertyName("suite_version")]
    public string SuiteVersion { get; init; } = string.Empty;

    [JsonPropertyName("profile")]
    public string Profile { get; init; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    [JsonPropertyName("cpu_name")]
    public string CpuName { get; init; } = string.Empty;

    [JsonPropertyName("cpu_cores")]
    public int? CpuCores { get; init; }

    [JsonPropertyName("cpu_threads")]
    public int? CpuThreads { get; init; }

    [JsonPropertyName("hardware")]
    public BenchmarkHardwareInfo? Hardware { get; init; }

    [JsonPropertyName("scores")]
    public IReadOnlyList<BenchmarkValueDto>? Scores { get; init; }

    [JsonPropertyName("metrics")]
    public IReadOnlyList<BenchmarkValueDto>? Metrics { get; init; }

    [JsonPropertyName("has_detail")]
    public bool HasDetail { get; init; }

    [JsonPropertyName("has_telemetry")]
    public bool HasTelemetry { get; init; }

    [JsonPropertyName("client_created_at")]
    public string? ClientCreatedAt { get; init; }

    [JsonPropertyName("server_created_at")]
    public string? ServerCreatedAt { get; init; }

    [JsonPropertyName("telemetry_samples")]
    public IReadOnlyList<BenchmarkTelemetrySample>? TelemetrySamples { get; init; }
}

public sealed class BenchmarkRunDetail
{
    [JsonPropertyName("run_id")]
    public string RunId { get; init; } = string.Empty;

    [JsonPropertyName("suite_id")]
    public string SuiteId { get; init; } = BenchmarkPayload.CpuSuiteId;

    [JsonPropertyName("suite_version")]
    public string SuiteVersion { get; init; } = string.Empty;

    [JsonPropertyName("profile")]
    public string Profile { get; init; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("hardware")]
    public BenchmarkHardwareInfo? Hardware { get; init; }

    [JsonPropertyName("scores")]
    public IReadOnlyList<BenchmarkValueDto> Scores { get; init; } = [];

    [JsonPropertyName("metrics")]
    public IReadOnlyList<BenchmarkValueDto> Metrics { get; init; } = [];
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

    [JsonPropertyName("cpu_core_clocks_ghz")]
    public IReadOnlyList<double>? CpuCoreClocksGhz { get; init; }
}

public enum BenchmarkUploadStatus
{
    Uploaded,
    Updated,
    Duplicate,
    RateLimited,
    Invalid,
    Failed
}

public sealed record BenchmarkUploadResult(
    BenchmarkUploadStatus Status,
    string Message,
    HttpStatusCode? HttpStatusCode = null,
    string RunId = "",
    string OwnerToken = "");

public sealed class BenchmarkDeleteRequest
{
    public string RunId { get; set; } = string.Empty;

    public string OwnerToken { get; set; } = string.Empty;
}

public enum BenchmarkDeleteStatus
{
    Deleted,
    NotFound,
    RateLimited,
    Invalid,
    Failed
}

public sealed record BenchmarkDeleteResult(
    BenchmarkDeleteStatus Status,
    string Message,
    HttpStatusCode? HttpStatusCode = null);

internal sealed class BenchmarkRunInitResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("run_id")]
    public string RunId { get; init; } = string.Empty;

    [JsonPropertyName("owner_token")]
    public string OwnerToken { get; init; } = string.Empty;
}

public sealed class BenchmarkApiClient : IDisposable
{
    private static readonly Uri BaseEndpoint = new("https://remoooo.com/wp-json/remo-benchmark/v2/");
    private static readonly Uri InitEndpoint = new(BaseEndpoint, "runs/init");
    private static readonly Uri LeaderboardEndpoint = new(BaseEndpoint, "leaderboard");
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
        BenchmarkPayload.NormalizeMetrics(dto);
        if (!BenchmarkPayload.TryValidateForUpload(dto, out string validationError))
        {
            return new BenchmarkUploadResult(BenchmarkUploadStatus.Invalid, validationError);
        }

        BenchmarkRunInitResponse? init = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                init = await InitializeRunAsync(cancellationToken).ConfigureAwait(false);
                if (init is null || !BenchmarkPayload.IsValidRunId(init.RunId) || string.IsNullOrWhiteSpace(init.OwnerToken))
                {
                    return new BenchmarkUploadResult(BenchmarkUploadStatus.Failed, "Run init response did not include run_id and owner_token.");
                }

                BenchmarkUploadResult summary = await UploadSummaryAsync(init.RunId, init.OwnerToken, dto, cancellationToken).ConfigureAwait(false);
                if ((int?)summary.HttpStatusCode >= 500 && attempt < 2)
                {
                    await Task.Delay(RetryDelay(attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (summary.Status is not (BenchmarkUploadStatus.Uploaded or BenchmarkUploadStatus.Updated or BenchmarkUploadStatus.Duplicate))
                {
                    return summary with { RunId = init.RunId, OwnerToken = init.OwnerToken };
                }

                if (dto.LocalTelemetrySamples is { Count: > 0 } samples)
                {
                    try
                    {
                        await UploadTelemetryAsync(init.RunId, init.OwnerToken, samples, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch
                    {
                        // The leaderboard summary is authoritative; telemetry can be retried by a future maintenance flow.
                    }
                }

                return summary with { RunId = init.RunId, OwnerToken = init.OwnerToken };
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
                return new BenchmarkUploadResult(
                    BenchmarkUploadStatus.Failed,
                    ex.Message,
                    RunId: init?.RunId ?? string.Empty,
                    OwnerToken: init?.OwnerToken ?? string.Empty);
            }
        }

        return new BenchmarkUploadResult(
            BenchmarkUploadStatus.Failed,
            "Upload retry limit reached.",
            RunId: init?.RunId ?? string.Empty,
            OwnerToken: init?.OwnerToken ?? string.Empty);
    }

    public async Task<BenchmarkDeleteResult> DeleteAsync(BenchmarkDeleteRequest request, CancellationToken cancellationToken)
    {
        if (!BenchmarkPayload.TryValidateForDelete(request, out string validationError))
        {
            return new BenchmarkDeleteResult(BenchmarkDeleteStatus.Invalid, validationError);
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using HttpRequestMessage httpRequest = CreateAuthorizedRequest(HttpMethod.Delete, BuildRunUri(request.RunId), request.OwnerToken);
                using HttpResponseMessage response = await _http.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return InterpretDeleteSuccess(body, response.StatusCode);
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new BenchmarkDeleteResult(BenchmarkDeleteStatus.NotFound, body, response.StatusCode);
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    return new BenchmarkDeleteResult(BenchmarkDeleteStatus.RateLimited, body, response.StatusCode);
                }

                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    return new BenchmarkDeleteResult(BenchmarkDeleteStatus.Invalid, body, response.StatusCode);
                }

                if ((int)response.StatusCode >= 500 && attempt < 2)
                {
                    await Task.Delay(RetryDelay(attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return new BenchmarkDeleteResult(BenchmarkDeleteStatus.Failed, body, response.StatusCode);
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
                return new BenchmarkDeleteResult(BenchmarkDeleteStatus.Failed, ex.Message);
            }
        }

        return new BenchmarkDeleteResult(BenchmarkDeleteStatus.Failed, "Delete retry limit reached.");
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
                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return DeserializeLeaderboard(body);
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

    public async Task<BenchmarkRunDetail?> GetRunAsync(string runId, CancellationToken cancellationToken)
    {
        if (!BenchmarkPayload.IsValidRunId(runId))
        {
            return null;
        }

        Uri requestUri = new($"{BuildRunUri(runId)}?include=scores,metrics,hardware");
        using HttpResponseMessage response = await _http.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<BenchmarkRunDetail>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BenchmarkTelemetrySample>> GetTelemetryAsync(string runId, int maxPoints, CancellationToken cancellationToken)
    {
        if (!BenchmarkPayload.IsValidRunId(runId))
        {
            return [];
        }

        StringBuilder query = new();
        AppendQuery(query, "series", "cpu.load_percent,cpu.max_temperature_c,cpu.package_power_w,cpu.clock_ghz");
        AppendQuery(query, "max_points", Math.Clamp(maxPoints, 1, BenchmarkPayload.MaxTelemetrySamples).ToString(CultureInfo.InvariantCulture));
        Uri requestUri = new($"{BuildTelemetryUri(runId)}?{query}");
        using HttpResponseMessage response = await _http.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return DeserializeTelemetry(body);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }

    private async Task<BenchmarkRunInitResponse?> InitializeRunAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _http.PostAsync(InitEndpoint, new StringContent("{}", Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<BenchmarkRunInitResponse>(body, JsonOptions);
    }

    private async Task<BenchmarkUploadResult> UploadSummaryAsync(string runId, string ownerToken, BenchmarkUploadDto dto, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Put, BuildRunUri(runId), ownerToken);
        request.Content = JsonContent.Create(dto, options: JsonOptions);
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode && TryReadResponseStatus(body, out string responseStatus))
        {
            if (responseStatus.Equals("already_finalized", StringComparison.OrdinalIgnoreCase)
                || responseStatus.Equals("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return new BenchmarkUploadResult(BenchmarkUploadStatus.Duplicate, body, response.StatusCode, runId, ownerToken);
            }

            if (responseStatus.Equals("updated", StringComparison.OrdinalIgnoreCase))
            {
                return new BenchmarkUploadResult(BenchmarkUploadStatus.Updated, body, response.StatusCode, runId, ownerToken);
            }

            if (responseStatus.Equals("finalized", StringComparison.OrdinalIgnoreCase)
                || responseStatus.Equals("uploaded", StringComparison.OrdinalIgnoreCase)
                || responseStatus.Equals("created", StringComparison.OrdinalIgnoreCase))
            {
                return new BenchmarkUploadResult(BenchmarkUploadStatus.Uploaded, body, response.StatusCode, runId, ownerToken);
            }
        }

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Created)
        {
            return new BenchmarkUploadResult(BenchmarkUploadStatus.Uploaded, body, response.StatusCode, runId, ownerToken);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new BenchmarkUploadResult(BenchmarkUploadStatus.RateLimited, body, response.StatusCode, runId, ownerToken);
        }

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new BenchmarkUploadResult(BenchmarkUploadStatus.Invalid, body, response.StatusCode, runId, ownerToken);
        }

        return new BenchmarkUploadResult(BenchmarkUploadStatus.Failed, body, response.StatusCode, runId, ownerToken);
    }

    private async Task UploadTelemetryAsync(string runId, string ownerToken, IReadOnlyList<BenchmarkTelemetrySample> samples, CancellationToken cancellationToken)
    {
        BenchmarkTelemetryChunkDto chunk = BenchmarkPayload.BuildTelemetryChunk(samples);
        if (chunk.Series.Count == 0)
        {
            return;
        }

        using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Put, BuildTelemetryUri(runId), ownerToken);
        request.Content = JsonContent.Create(chunk, options: JsonOptions);
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return;
        }

        if ((int)response.StatusCode >= 500)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private static IReadOnlyList<BenchmarkLeaderboardEntry> DeserializeLeaderboard(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        try
        {
            BenchmarkLeaderboardResponse? response = JsonSerializer.Deserialize<BenchmarkLeaderboardResponse>(body, JsonOptions);
            if (response?.Items is { Count: > 0 } items)
            {
                return items;
            }

            using JsonDocument document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<BenchmarkLeaderboardEntry>>(body, JsonOptions) ?? [];
            }
        }
        catch (JsonException)
        {
            return [];
        }

        return [];
    }

    private static IReadOnlyList<BenchmarkTelemetrySample> DeserializeTelemetry(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("samples", out JsonElement samplesElement)
                && samplesElement.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<BenchmarkTelemetrySample>>(samplesElement.GetRawText(), JsonOptions) ?? [];
            }

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("series", out JsonElement seriesElement)
                && seriesElement.ValueKind == JsonValueKind.Object)
            {
                return DeserializeTelemetrySeries(seriesElement);
            }
        }
        catch (JsonException)
        {
            return [];
        }

        return [];
    }

    private static IReadOnlyList<BenchmarkTelemetrySample> DeserializeTelemetrySeries(JsonElement seriesElement)
    {
        SortedDictionary<double, MutableTelemetrySample> samples = [];
        foreach (JsonProperty property in seriesElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (JsonElement point in property.Value.EnumerateArray())
            {
                if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() < 2)
                {
                    continue;
                }

                JsonElement elapsedElement = point[0];
                JsonElement valueElement = point[1];
                if (!elapsedElement.TryGetDouble(out double elapsed) || !valueElement.TryGetDouble(out double value))
                {
                    continue;
                }

                if (!samples.TryGetValue(elapsed, out MutableTelemetrySample? sample))
                {
                    sample = new MutableTelemetrySample(elapsed);
                    samples[elapsed] = sample;
                }

                sample.Set(property.Name, value);
            }
        }

        return samples.Values
            .Select(sample => sample.ToTelemetrySample())
            .Where(sample => sample is not null)
            .Cast<BenchmarkTelemetrySample>()
            .ToArray();
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, Uri uri, string ownerToken)
    {
        HttpRequestMessage request = new(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        request.Headers.TryAddWithoutValidation("X-Remo-Owner-Token", ownerToken);
        return request;
    }

    private static Uri BuildRunUri(string runId) => new(BaseEndpoint, $"runs/{Uri.EscapeDataString(runId)}");

    private static Uri BuildTelemetryUri(string runId) => new(BaseEndpoint, $"runs/{Uri.EscapeDataString(runId)}/telemetry");

    private static Uri BuildLeaderboardUri(BenchmarkLeaderboardQuery query)
    {
        StringBuilder builder = new();
        AppendQuery(builder, "suite_id", query.SuiteId);
        AppendQuery(builder, "suite_version", query.SuiteVersion);
        AppendQuery(builder, "profile", query.Profile);
        AppendQuery(builder, "mode", query.Mode);
        AppendQuery(builder, "ranking", BenchmarkPayload.ScoreKindToApiValue(query.ScoreKind));
        AppendQuery(builder, "limit", Math.Clamp(query.Limit, 1, 100).ToString(CultureInfo.InvariantCulture));
        AppendQuery(builder, "cursor", query.Cursor);
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

    private static TimeSpan RetryDelay(int attempt) => TimeSpan.FromMilliseconds(attempt == 0 ? 500 : 1500);

    private static bool IsTransientUploadException(Exception ex)
    {
        return ex is HttpRequestException or TaskCanceledException or IOException;
    }

    private static BenchmarkDeleteResult InterpretDeleteSuccess(string body, HttpStatusCode statusCode)
    {
        if (string.IsNullOrWhiteSpace(body)
            || TryReadResponseStatus(body, out string status)
            && status.Equals("deleted", StringComparison.OrdinalIgnoreCase))
        {
            return new BenchmarkDeleteResult(BenchmarkDeleteStatus.Deleted, body, statusCode);
        }

        if (TryReadSuccessFlag(body, out bool success))
        {
            return success
                ? new BenchmarkDeleteResult(BenchmarkDeleteStatus.Deleted, body, statusCode)
                : new BenchmarkDeleteResult(BenchmarkDeleteStatus.Failed, body, statusCode);
        }

        return new BenchmarkDeleteResult(BenchmarkDeleteStatus.Deleted, body, statusCode);
    }

    private static bool TryReadResponseStatus(string body, out string status)
    {
        status = string.Empty;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("status", out JsonElement value)
                || value.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            status = value.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(status);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadSuccessFlag(string body, out bool success)
    {
        success = false;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("success", out JsonElement value)
                || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return false;
            }

            success = value.GetBoolean();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class MutableTelemetrySample(double elapsedSeconds)
    {
        private readonly double _elapsedSeconds = elapsedSeconds;

        private double? _loadPercent;
        private double? _temperatureC;
        private double? _packagePowerW;
        private double? _clockGhz;

        public void Set(string key, double value)
        {
            switch (key)
            {
                case "cpu.load_percent":
                    _loadPercent = value;
                    break;
                case "cpu.max_temperature_c":
                    _temperatureC = value;
                    break;
                case "cpu.package_power_w":
                    _packagePowerW = value;
                    break;
                case "cpu.clock_ghz":
                    _clockGhz = value;
                    break;
            }
        }

        public BenchmarkTelemetrySample? ToTelemetrySample()
        {
            if (!double.IsFinite(_elapsedSeconds) || _elapsedSeconds < 0)
            {
                return null;
            }

            return new BenchmarkTelemetrySample
            {
                ElapsedSeconds = _elapsedSeconds,
                CpuLoadPercent = _loadPercent,
                CpuMaxTemperatureC = _temperatureC,
                CpuPackagePowerW = _packagePowerW,
                CpuClockGhz = _clockGhz
            };
        }
    }
}

public static class BenchmarkPayload
{
    public const int SchemaVersion = 1;
    public const string CpuSuiteId = "cpu";
    public const string ValidationOk = "OK";
    public const string DefaultDisplayName = "Anonymous";
    public const string CpuCoreScoreKey = "cpu_core";
    public const string CpuMixedScoreKey = "cpu_mixed";
    public const string SciMarkMetricKey = "scimark.total";
    public const string ZstdCompressMetricKey = "zstd.compress_gbps";
    public const string ZstdDecompressMetricKey = "zstd.decompress_gbps";
    public const string XxHash3MetricKey = "xxhash3.gbps";
    public const string CpuAverageFrequencyMetricKey = "cpu.avg_frequency_ghz";
    public const string CpuMaxTemperatureMetricKey = "cpu.max_temperature_c";
    public const int MaxTelemetrySamples = 240;
    private const int MaxDisplayNameLength = 40;
    private const int MaxTelemetryCoreClocks = 256;
    private const double MaxAcceptedScore = 100_000_000d;

    public static string ProfileToApiValue(BenchmarkRunProfile profile) => profile switch
    {
        BenchmarkRunProfile.Quick => "quick",
        BenchmarkRunProfile.Sustained => "sustained",
        _ => "standard"
    };

    public static string ModeToApiValue(int workerCount) => workerCount == 1 ? "single" : "multi";

    public static string ScoreKindToApiValue(BenchmarkScoreKind scoreKind) => scoreKind switch
    {
        BenchmarkScoreKind.CpuMixed => CpuMixedScoreKey,
        _ => CpuCoreScoreKey
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
        dto.SuiteId = string.IsNullOrWhiteSpace(dto.SuiteId) ? CpuSuiteId : dto.SuiteId.Trim();
        dto.SuiteVersion = BenchmarkRunner.NormalizeVersion(dto.SuiteVersion);
        dto.AppVersion = dto.AppVersion?.Trim() ?? string.Empty;
        dto.Profile = dto.Profile?.Trim() ?? string.Empty;
        dto.Mode = dto.Mode?.Trim() ?? string.Empty;
        dto.DisplayName = NormalizeDisplayName(dto.DisplayName);
        dto.ClientCreatedAt = string.IsNullOrWhiteSpace(dto.ClientCreatedAt)
            ? UtcTimestamp(DateTimeOffset.UtcNow)
            : dto.ClientCreatedAt.Trim();
        dto.Scores = NormalizeValues(dto.Scores, scoreValues: true);
        dto.Metrics = NormalizeValues(dto.Metrics, scoreValues: false);
        dto.LocalTelemetrySamples = NormalizeTelemetrySamples(dto.LocalTelemetrySamples);
        dto.TelemetryManifest = dto.LocalTelemetrySamples is { Count: > 0 }
            ? new BenchmarkTelemetryManifest
            {
                Available = true,
                Series =
                [
                    "cpu.load_percent",
                    "cpu.max_temperature_c",
                    "cpu.package_power_w",
                    "cpu.clock_ghz"
                ]
            }
            : new BenchmarkTelemetryManifest { Available = false };
    }

    public static bool TryValidateForUpload(BenchmarkUploadDto dto, out string error)
    {
        error = string.Empty;
        if (dto.SchemaVersion != SchemaVersion)
        {
            error = "Unsupported benchmark payload schema.";
            return false;
        }

        if (!IsSafeToken(dto.SuiteId)
            || string.IsNullOrWhiteSpace(dto.AppVersion)
            || string.IsNullOrWhiteSpace(dto.SuiteVersion)
            || string.IsNullOrWhiteSpace(dto.Profile)
            || string.IsNullOrWhiteSpace(dto.Mode))
        {
            error = "Missing benchmark identity fields.";
            return false;
        }

        if (!BenchmarkRunner.IsSupportedVersion(dto.SuiteVersion))
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

        if (dto.Scores.Count == 0 || dto.Scores.Any(value => !IsSafeToken(value.Key) || !IsAcceptedScore(value.Value)))
        {
            error = "Invalid benchmark score.";
            return false;
        }

        if (dto.Metrics.Any(value => !IsSafeToken(value.Key) || !IsAcceptedMetric(value.Value)))
        {
            error = "Invalid benchmark metric.";
            return false;
        }

        if (!dto.Scores.Any(score => score.Key == CpuCoreScoreKey && IsAcceptedScore(score.Value)))
        {
            error = "Missing CPU core score.";
            return false;
        }

        if (!dto.Scores.Any(score => score.Key == CpuMixedScoreKey && IsAcceptedScore(score.Value)))
        {
            error = "Missing CPU mixed score.";
            return false;
        }

        if (!TelemetrySamplesAreValid(dto.LocalTelemetrySamples))
        {
            error = "Invalid benchmark telemetry.";
            return false;
        }

        return true;
    }

    public static bool TryValidateForDelete(BenchmarkDeleteRequest request, out string error)
    {
        error = string.Empty;
        request.RunId = request.RunId?.Trim() ?? string.Empty;
        request.OwnerToken = request.OwnerToken?.Trim() ?? string.Empty;
        if (!IsValidRunId(request.RunId))
        {
            error = "Invalid run id.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.OwnerToken))
        {
            error = "Missing owner token.";
            return false;
        }

        return true;
    }

    public static bool IsValidRunId(string? runId)
    {
        return !string.IsNullOrWhiteSpace(runId)
            && runId.Length <= 64
            && runId.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_');
    }

    public static BenchmarkTelemetryChunkDto BuildTelemetryChunk(IReadOnlyList<BenchmarkTelemetrySample> samples)
    {
        List<double[]> load = [];
        List<double[]> temperature = [];
        List<double[]> power = [];
        List<double[]> clock = [];
        foreach (BenchmarkTelemetrySample sample in samples.Take(MaxTelemetrySamples))
        {
            double elapsed = RoundMetric(sample.ElapsedSeconds, 3);
            AddTelemetryPoint(load, elapsed, sample.CpuLoadPercent, 2);
            AddTelemetryPoint(temperature, elapsed, sample.CpuMaxTemperatureC, 2);
            AddTelemetryPoint(power, elapsed, sample.CpuPackagePowerW, 3);
            AddTelemetryPoint(clock, elapsed, sample.CpuClockGhz, 3);
        }

        Dictionary<string, IReadOnlyList<double[]>> series = [];
        AddSeries(series, "cpu.load_percent", load);
        AddSeries(series, "cpu.max_temperature_c", temperature);
        AddSeries(series, "cpu.package_power_w", power);
        AddSeries(series, "cpu.clock_ghz", clock);
        return new BenchmarkTelemetryChunkDto { ChunkIndex = 0, Series = series };
    }

    private static IReadOnlyList<BenchmarkValueDto> NormalizeValues(IReadOnlyList<BenchmarkValueDto>? values, bool scoreValues)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        Dictionary<string, BenchmarkValueDto> normalized = new(StringComparer.Ordinal);
        foreach (BenchmarkValueDto value in values)
        {
            string key = value.Key?.Trim() ?? string.Empty;
            if (!IsSafeToken(key) || !double.IsFinite(value.Value))
            {
                continue;
            }

            double rounded = RoundMetric(value.Value, scoreValues ? 2 : 6);
            normalized[key] = new BenchmarkValueDto
            {
                Key = key,
                Value = rounded,
                Unit = string.IsNullOrWhiteSpace(value.Unit) ? null : value.Unit.Trim()
            };
        }

        return normalized.Values.OrderBy(value => value.Key, StringComparer.Ordinal).ToArray();
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
                CpuClockGhz = RoundNullable(sample.CpuClockGhz, 3),
                CpuCoreClocksGhz = NormalizeCoreClocks(sample.CpuCoreClocksGhz)
            });
        }

        return normalized.Count == 0 ? null : normalized;
    }

    private static bool IsSafeToken(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= 64
            && value.All(ch => char.IsLower(ch) || char.IsDigit(ch) || ch is '_' or '-' or '.');
    }

    private static double? RoundNullable(double? value, int digits = 6)
    {
        return value is { } actual && double.IsFinite(actual)
            ? RoundMetric(actual, digits)
            : null;
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
                || !IsAcceptedTelemetryValue(sample.CpuClockGhz, 10)
                || !CoreClocksAreValid(sample.CpuCoreClocksGhz))
            {
                return false;
            }

            previousElapsed = sample.ElapsedSeconds;
        }

        return true;
    }

    private static IReadOnlyList<double>? NormalizeCoreClocks(IReadOnlyList<double>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        List<double> normalized = new(Math.Min(values.Count, MaxTelemetryCoreClocks));
        for (int i = 0; i < values.Count && normalized.Count < MaxTelemetryCoreClocks; i++)
        {
            double value = values[i];
            if (double.IsFinite(value) && value is >= 0 and <= 10)
            {
                normalized.Add(RoundMetric(value, 3));
            }
        }

        return normalized.Count == 0 ? null : normalized;
    }

    private static bool CoreClocksAreValid(IReadOnlyList<double>? values)
    {
        if (values is null)
        {
            return true;
        }

        return values.Count <= MaxTelemetryCoreClocks
            && values.All(value => IsAcceptedTelemetryValue(value, 10));
    }

    private static void AddTelemetryPoint(List<double[]> points, double elapsed, double? value, int digits)
    {
        if (value is not { } actual || !double.IsFinite(actual))
        {
            return;
        }

        points.Add([elapsed, RoundMetric(actual, digits)]);
    }

    private static void AddSeries(Dictionary<string, IReadOnlyList<double[]>> series, string key, List<double[]> points)
    {
        if (points.Count > 0)
        {
            series[key] = points;
        }
    }
}
