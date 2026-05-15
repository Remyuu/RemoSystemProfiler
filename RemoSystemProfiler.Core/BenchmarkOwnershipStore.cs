using System.Text.Json;
using System.Text.Json.Serialization;

namespace RemoSystemProfiler.Core;

public sealed record BenchmarkOwnerCredential(string RunId, string OwnerToken);

public sealed class BenchmarkOwnerRecord
{
    [JsonPropertyName("run_id")]
    public string RunId { get; set; } = string.Empty;

    [JsonPropertyName("owner_token")]
    public string OwnerToken { get; set; } = string.Empty;

    [JsonPropertyName("suite_id")]
    public string SuiteId { get; set; } = BenchmarkPayload.CpuSuiteId;

    [JsonPropertyName("suite_version")]
    public string SuiteVersion { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = BenchmarkPayload.DefaultDisplayName;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}

public static class BenchmarkOwnershipStore
{
    private const string DirectoryName = "benchmark-results";
    private const string FileName = "benchmark-owners.json";
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static bool TryGetCredential(string? runId, out BenchmarkOwnerCredential credential)
    {
        credential = new BenchmarkOwnerCredential(string.Empty, string.Empty);
        if (!BenchmarkPayload.IsValidRunId(runId))
        {
            return false;
        }

        lock (Gate)
        {
            BenchmarkOwnerStoreDocument document = LoadDocument();
            BenchmarkOwnerRecord? record = document.Runs.FirstOrDefault(item => string.Equals(item.RunId, runId, StringComparison.Ordinal));
            if (record is null || string.IsNullOrWhiteSpace(record.OwnerToken))
            {
                return false;
            }

            credential = new BenchmarkOwnerCredential(record.RunId, record.OwnerToken);
            return true;
        }
    }

    public static bool HasCredential(string? runId) => TryGetCredential(runId, out _);

    public static void Save(BenchmarkOwnerRecord record)
    {
        if (!BenchmarkPayload.IsValidRunId(record.RunId) || string.IsNullOrWhiteSpace(record.OwnerToken))
        {
            return;
        }

        lock (Gate)
        {
            BenchmarkOwnerStoreDocument document = LoadDocument();
            document.Runs.RemoveAll(item => string.Equals(item.RunId, record.RunId, StringComparison.Ordinal));
            document.Runs.Add(Normalize(record));
            SaveDocument(document);
        }
    }

    public static void Remove(string? runId)
    {
        if (!BenchmarkPayload.IsValidRunId(runId))
        {
            return;
        }

        lock (Gate)
        {
            BenchmarkOwnerStoreDocument document = LoadDocument();
            if (document.Runs.RemoveAll(item => string.Equals(item.RunId, runId, StringComparison.Ordinal)) > 0)
            {
                SaveDocument(document);
            }
        }
    }

    private static BenchmarkOwnerRecord Normalize(BenchmarkOwnerRecord record)
    {
        return new BenchmarkOwnerRecord
        {
            RunId = record.RunId.Trim(),
            OwnerToken = record.OwnerToken.Trim(),
            SuiteId = string.IsNullOrWhiteSpace(record.SuiteId) ? BenchmarkPayload.CpuSuiteId : record.SuiteId.Trim(),
            SuiteVersion = record.SuiteVersion?.Trim() ?? string.Empty,
            DisplayName = BenchmarkPayload.NormalizeDisplayName(record.DisplayName),
            CreatedAt = string.IsNullOrWhiteSpace(record.CreatedAt)
                ? BenchmarkPayload.UtcTimestamp(DateTimeOffset.UtcNow)
                : record.CreatedAt.Trim()
        };
    }

    private static BenchmarkOwnerStoreDocument LoadDocument()
    {
        try
        {
            string path = StorePath;
            if (!File.Exists(path))
            {
                return new BenchmarkOwnerStoreDocument();
            }

            BenchmarkOwnerStoreDocument? document = JsonSerializer.Deserialize<BenchmarkOwnerStoreDocument>(File.ReadAllText(path), SerializerOptions);
            return document ?? new BenchmarkOwnerStoreDocument();
        }
        catch
        {
            return new BenchmarkOwnerStoreDocument();
        }
    }

    private static void SaveDocument(BenchmarkOwnerStoreDocument document)
    {
        try
        {
            string path = StorePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppContext.BaseDirectory);
            File.WriteAllText(path, JsonSerializer.Serialize(document, SerializerOptions));
        }
        catch
        {
            // Losing local delete ownership should not interrupt the benchmark UI.
        }
    }

    private static string StorePath => Path.Combine(AppContext.BaseDirectory, DirectoryName, FileName);

    private sealed class BenchmarkOwnerStoreDocument
    {
        [JsonPropertyName("runs")]
        public List<BenchmarkOwnerRecord> Runs { get; set; } = [];
    }
}
