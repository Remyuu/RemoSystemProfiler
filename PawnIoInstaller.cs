using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Principal;

namespace RemoSystemProfiler;

internal sealed record PawnIoInstallResult(
    bool IsSuccess,
    bool RequiresRestart,
    string Message);

internal static class PawnIoInstaller
{
    private const string OfficialInstallerUrl = "https://github.com/namazso/PawnIO.Setup/releases/latest/download/PawnIO_setup.exe";
    private const string InstallerFileName = "PawnIO_setup.exe";
    private static readonly Version DirectUpgradeMinimumVersion = new(2, 1, 0);
    private static readonly HttpClient Client = CreateClient();

    private sealed record PawnIoInstallRecord(string? DisplayVersion);

    private sealed record InstallerRunResult(bool IsSuccess, bool RequiresRestart, int? ExitCode, string? ErrorMessage);

    public static async Task<PawnIoInstallResult> DownloadAndInstallLatestAsync(
        IProgress<DownloadProgressInfo>? progress,
        CancellationToken token)
    {
        string installDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoSystemProfiler",
            "PawnIO",
            DateTimeOffset.Now.ToString("yyyyMMddHHmmss"));
        Directory.CreateDirectory(installDirectory);

        string installerPath = Path.Combine(installDirectory, InstallerFileName);
        await DownloadInstallerAsync(installerPath, progress, token).ConfigureAwait(false);

        PawnIoInstallRecord? existingInstall = ReadInstallRecord();
        if (IsLegacyInstall(existingInstall))
        {
            InstallerRunResult uninstallResult = await RunInstallerAsync(installerPath, "-uninstall", token).ConfigureAwait(false);
            if (!uninstallResult.IsSuccess)
            {
                return new PawnIoInstallResult(
                    false,
                    false,
                    Localization.PawnIoOldInstallRemovalFailed(DescribeInstallerFailure(uninstallResult)));
            }

            if (uninstallResult.RequiresRestart)
            {
                return new PawnIoInstallResult(true, true, Localization.PawnIoOldInstallRemovedRestart);
            }
        }

        InstallerRunResult installResult = await RunInstallerAsync(installerPath, "-install", token).ConfigureAwait(false);
        if (!installResult.IsSuccess)
        {
            return new PawnIoInstallResult(
                false,
                false,
                Localization.PawnIoInstallFailed(DescribeInstallerFailure(installResult)));
        }

        return new PawnIoInstallResult(
            true,
            installResult.RequiresRestart,
            installResult.RequiresRestart ? Localization.PawnIoInstallRestartRequired : Localization.PawnIoInstallCompleted);
    }

    private static async Task DownloadInstallerAsync(
        string destinationPath,
        IProgress<DownloadProgressInfo>? progress,
        CancellationToken token)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, OfficialInstallerUrl);
        using HttpResponseMessage response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? ".");
        await using Stream source = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        await using FileStream destination = File.Create(destinationPath);

        long totalBytes = response.Content.Headers.ContentLength ?? 0;
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
            double percent = totalBytes > 0
                ? Math.Clamp(downloadedBytes * 100d / totalBytes, 0, 100)
                : 0;
            progress?.Report(new DownloadProgressInfo(percent, downloadedBytes, totalBytes, bytesPerSecond));
        }

        double finalSpeed = stopwatch.Elapsed.TotalSeconds > 0 ? downloadedBytes / stopwatch.Elapsed.TotalSeconds : 0;
        progress?.Report(new DownloadProgressInfo(100, downloadedBytes, totalBytes, finalSpeed));
    }

    private static async Task<InstallerRunResult> RunInstallerAsync(
        string installerPath,
        string actionArgument,
        CancellationToken token)
    {
        ProcessStartInfo startInfo = new(installerPath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(installerPath),
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (!IsRunningAsAdministrator())
        {
            startInfo.Verb = "runas";
        }

        startInfo.ArgumentList.Add(actionArgument);
        startInfo.ArgumentList.Add("-silent");

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return new InstallerRunResult(false, false, null, "Installer did not start.");
            }

            await process.WaitForExitAsync(token).ConfigureAwait(false);
            bool requiresRestart = process.ExitCode is 3010 or 1641;
            bool success = process.ExitCode == 0 || requiresRestart;
            return new InstallerRunResult(success, requiresRestart, process.ExitCode, null);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new InstallerRunResult(false, false, null, Localization.PawnIoInstallCanceled);
        }
        catch (Win32Exception ex)
        {
            return new InstallerRunResult(false, false, null, ex.Message);
        }
    }

    private static string DescribeInstallerFailure(InstallerRunResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return result.ErrorMessage;
        }

        return result.ExitCode.HasValue
            ? $"Installer exit code {result.ExitCode.Value}."
            : "Unknown installer error.";
    }

    private static PawnIoInstallRecord? ReadInstallRecord()
    {
        return ReadInstallRecord(RegistryView.Registry64)
            ?? ReadInstallRecord(RegistryView.Registry32);
    }

    private static PawnIoInstallRecord? ReadInstallRecord(RegistryView view)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using RegistryKey? directKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO");
            PawnIoInstallRecord? directRecord = ReadInstallRecord(directKey);
            if (directRecord is not null)
            {
                return directRecord;
            }

            using RegistryKey? uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallKey is null)
            {
                return null;
            }

            foreach (string subKeyName in uninstallKey.GetSubKeyNames())
            {
                using RegistryKey? appKey = uninstallKey.OpenSubKey(subKeyName);
                PawnIoInstallRecord? record = ReadInstallRecord(appKey);
                if (record is not null)
                {
                    return record;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static PawnIoInstallRecord? ReadInstallRecord(RegistryKey? appKey)
    {
        if (appKey?.GetValue("DisplayName") is not string displayName
            || !displayName.Contains("PawnIO", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new PawnIoInstallRecord(appKey.GetValue("DisplayVersion") as string);
    }

    private static bool IsLegacyInstall(PawnIoInstallRecord? record)
    {
        return record?.DisplayVersion is { } versionText
            && TryParseVersion(versionText, out Version? version)
            && version < DirectUpgradeMinimumVersion;
    }

    private static bool TryParseVersion(string text, out Version? version)
    {
        string normalized = text.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        int suffixIndex = normalized.IndexOfAny(['-', '+']);
        if (suffixIndex >= 0)
        {
            normalized = normalized[..suffixIndex];
        }

        string[] parts = normalized
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(4)
            .ToArray();
        if (parts.Length < 2)
        {
            version = null;
            return false;
        }

        int[] parsed = new int[4];
        for (int i = 0; i < parsed.Length; i++)
        {
            if (i >= parts.Length)
            {
                parsed[i] = 0;
                continue;
            }

            if (!int.TryParse(parts[i], out parsed[i]))
            {
                version = null;
                return false;
            }
        }

        version = new Version(parsed[0], parsed[1], parsed[2], parsed[3]);
        return true;
    }

    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static HttpClient CreateClient()
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromMinutes(2)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RemoSystemProfiler", "1.0"));
        return client;
    }
}
