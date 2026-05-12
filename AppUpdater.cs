using System.Diagnostics;
using System.IO.Compression;

namespace RemoSystemProfiler;

internal sealed record UpdateInstallResult(
    bool IsSuccess,
    bool ShouldCloseApplication,
    string Message);

internal static class AppUpdater
{
    private static readonly string UpdatesRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RemoSystemProfiler",
        "Updates");

    public static async Task<UpdateInstallResult> DownloadAndStartUpdateAsync(
        GitHubReleaseAssetInfo asset,
        IProgress<DownloadProgressInfo>? progress,
        CancellationToken token)
    {
        string updateDirectory = Path.Combine(UpdatesRoot, DateTimeOffset.Now.ToString("yyyyMMddHHmmss"));
        Directory.CreateDirectory(updateDirectory);

        string assetPath = Path.Combine(updateDirectory, SanitizeFileName(asset.Name));
        await GitHubReleaseChecker.DownloadAssetAsync(asset, assetPath, progress, token).ConfigureAwait(false);

        string extension = Path.GetExtension(assetPath).ToLowerInvariant();
        return extension switch
        {
            ".zip" => StartZipSelfUpdater(assetPath, updateDirectory),
            ".msi" or ".exe" => StartInstaller(assetPath),
            _ => new UpdateInstallResult(false, false, Localization.UpdateUnsupportedAsset(asset.Name))
        };
    }

    private static UpdateInstallResult StartZipSelfUpdater(string archivePath, string updateDirectory)
    {
        if (!IsZipArchiveReadable(archivePath))
        {
            return new UpdateInstallResult(false, false, Localization.UpdateInvalidPackage);
        }

        string? executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            executablePath = Path.Combine(AppContext.BaseDirectory, "RemoSystemProfiler.exe");
        }

        if (!File.Exists(executablePath))
        {
            return new UpdateInstallResult(false, false, Localization.UpdateCannotFindExecutable);
        }

        string scriptPath = Path.Combine(updateDirectory, "apply-update.ps1");
        File.WriteAllText(scriptPath, BuildUpdaterScript());

        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-ProcessId");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
        startInfo.ArgumentList.Add("-ArchivePath");
        startInfo.ArgumentList.Add(archivePath);
        startInfo.ArgumentList.Add("-TargetDir");
        startInfo.ArgumentList.Add(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        startInfo.ArgumentList.Add("-ExePath");
        startInfo.ArgumentList.Add(executablePath);

        Process.Start(startInfo);
        return new UpdateInstallResult(true, true, Localization.UpdateWillRestart);
    }

    private static UpdateInstallResult StartInstaller(string installerPath)
    {
        Process.Start(new ProcessStartInfo(installerPath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(installerPath)
        });
        return new UpdateInstallResult(true, true, Localization.UpdateInstallerStarted);
    }

    private static bool IsZipArchiveReadable(string archivePath)
    {
        try
        {
            using ZipArchive archive = ZipFile.OpenRead(archivePath);
            return archive.Entries.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "update.zip" : fileName;
    }

    private static string BuildUpdaterScript() =>
        """
        param(
            [Parameter(Mandatory=$true)][int]$ProcessId,
            [Parameter(Mandatory=$true)][string]$ArchivePath,
            [Parameter(Mandatory=$true)][string]$TargetDir,
            [Parameter(Mandatory=$true)][string]$ExePath
        )

        $ErrorActionPreference = 'Stop'
        Wait-Process -Id $ProcessId -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 400

        $extractDir = Join-Path ([System.IO.Path]::GetTempPath()) ('RemoSystemProfiler-update-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Force -Path $extractDir | Out-Null
        Expand-Archive -LiteralPath $ArchivePath -DestinationPath $extractDir -Force

        $sourceDir = $extractDir
        $children = @(Get-ChildItem -LiteralPath $extractDir -Force)
        if ($children.Count -eq 1 -and $children[0].PSIsContainer) {
            $sourceDir = $children[0].FullName
        }

        $backupDir = Join-Path ([System.IO.Path]::GetTempPath()) ('RemoSystemProfiler-backup-' + (Get-Date -Format 'yyyyMMddHHmmss'))
        New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
        Copy-Item -Path (Join-Path $TargetDir '*') -Destination $backupDir -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item -Path (Join-Path $sourceDir '*') -Destination $TargetDir -Recurse -Force

        Start-Process -FilePath $ExePath -WorkingDirectory $TargetDir
        """;
}
