namespace RemoSystemProfiler.Core;

public static class ApplicationDataStore
{
    private const string AppDirectoryName = "RemoSystemProfiler";

    public static string RootDirectory
    {
        get
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return string.IsNullOrWhiteSpace(localAppData)
                ? Path.Combine(AppContext.BaseDirectory, AppDirectoryName)
                : Path.Combine(localAppData, AppDirectoryName);
        }
    }

    public static string BenchmarkResultsDirectory => Path.Combine(RootDirectory, "benchmark-results");

    public static string GetFilePath(string fileName) => Path.Combine(RootDirectory, fileName);

    public static void EnsureRootDirectory() => Directory.CreateDirectory(RootDirectory);
}
