using System.Text.Json;
using RemoSystemProfiler.Core;

namespace RemoSystemProfiler;

internal sealed record DashboardSettings(
    int ChartRangeIndex = 0,
    int UpdateIntervalIndex = 1,
    int ThemeIndex = 0,
    int LanguageIndex = 0,
    bool CpuOverallView = false,
    string BenchmarkDisplayName = BenchmarkPayload.DefaultDisplayName);

internal static class DashboardSettingsStore
{
    private const string FileName = "dashboard-settings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static string DataDirectory => ApplicationDataStore.RootDirectory;

    public static DashboardSettings Load()
    {
        try
        {
            string path = ResolveReadPath();
            if (!File.Exists(path))
            {
                return new DashboardSettings();
            }

            DashboardSettings? settings = JsonSerializer.Deserialize<DashboardSettings>(File.ReadAllText(path), SerializerOptions);
            return settings is null ? new DashboardSettings() : Normalize(settings);
        }
        catch
        {
            return new DashboardSettings();
        }
    }

    public static void Save(DashboardSettings settings)
    {
        try
        {
            string path = SettingsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(Normalize(settings), SerializerOptions));
        }
        catch
        {
            // Settings persistence should never interrupt hardware monitoring.
        }
    }

    private static DashboardSettings Normalize(DashboardSettings settings) => settings with
    {
        ChartRangeIndex = Math.Clamp(settings.ChartRangeIndex, 0, 3),
        UpdateIntervalIndex = Math.Clamp(settings.UpdateIntervalIndex, 0, 3),
        ThemeIndex = Math.Clamp(settings.ThemeIndex, 0, 2),
        LanguageIndex = Math.Clamp(settings.LanguageIndex, 0, 6),
        BenchmarkDisplayName = BenchmarkPayload.NormalizeDisplayName(settings.BenchmarkDisplayName)
    };

    public static void EnsureDataDirectory() => ApplicationDataStore.EnsureRootDirectory();

    private static string ResolveReadPath()
    {
        string path = SettingsPath;
        if (File.Exists(path))
        {
            return path;
        }

        string legacyPath = LegacySettingsPath;
        return File.Exists(legacyPath) ? legacyPath : path;
    }

    private static string SettingsPath => ApplicationDataStore.GetFilePath(FileName);

    private static string LegacySettingsPath => Path.Combine(AppContext.BaseDirectory, FileName);
}
