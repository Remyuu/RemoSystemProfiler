param(
    [string]$ProcessName = "RemoSystemProfiler",
    [int]$DurationMinutes = 10,
    [int]$IntervalSeconds = 5,
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

if ($DurationMinutes -lt 1) {
    throw "DurationMinutes must be at least 1."
}

if ($IntervalSeconds -lt 1) {
    throw "IntervalSeconds must be at least 1."
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    $outputDirectory = Join-Path $root ".tmp"
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = Join-Path $outputDirectory "memory-$ProcessName-$timestamp.csv"
}

function Get-PrivateWorkingSetBytes {
    param([int]$ProcessId)

    try {
        $counter = Get-CimInstance Win32_PerfFormattedData_PerfProc_Process -Filter "IDProcess = $ProcessId" -ErrorAction Stop |
            Select-Object -First 1
        if ($null -ne $counter -and $null -ne $counter.WorkingSetPrivate) {
            return [int64]$counter.WorkingSetPrivate
        }
    }
    catch {
        return $null
    }

    return $null
}

$endAt = (Get-Date).AddMinutes($DurationMinutes)
$wroteHeader = $false

while ((Get-Date) -le $endAt) {
    $sampledAt = Get-Date
    $processes = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Sort-Object Id

    foreach ($process in $processes) {
        $threadCount = 0
        try {
            $threadCount = $process.Threads.Count
        }
        catch {
            $threadCount = 0
        }

        $row = [pscustomobject]@{
            TimestampUtc = $sampledAt.ToUniversalTime().ToString("o")
            ProcessName = $process.ProcessName
            ProcessId = $process.Id
            PrivateBytes = [int64]$process.PrivateMemorySize64
            WorkingSet = [int64]$process.WorkingSet64
            PrivateWorkingSet = Get-PrivateWorkingSetBytes -ProcessId $process.Id
            Handles = [int]$process.HandleCount
            Threads = [int]$threadCount
            CpuSeconds = if ($null -ne $process.CPU) { [double]$process.CPU } else { 0 }
        }

        if ($wroteHeader) {
            $row | Export-Csv -Path $OutputPath -NoTypeInformation -Append
        }
        else {
            $row | Export-Csv -Path $OutputPath -NoTypeInformation
            $wroteHeader = $true
        }
    }

    Start-Sleep -Seconds $IntervalSeconds
}

if (-not $wroteHeader) {
    [pscustomobject]@{
        TimestampUtc = (Get-Date).ToUniversalTime().ToString("o")
        ProcessName = $ProcessName
        ProcessId = $null
        PrivateBytes = $null
        WorkingSet = $null
        PrivateWorkingSet = $null
        Handles = $null
        Threads = $null
        CpuSeconds = $null
    } | Export-Csv -Path $OutputPath -NoTypeInformation
}

Write-Output $OutputPath
