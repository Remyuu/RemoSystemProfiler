# Remo System Profiler

Compact Windows 11 hardware sensor dashboard built with WinUI 3.

## Scope

- Reads CPU, memory, GPU, and storage sensors through `LibreHardwareMonitorLib`.
- Does not require a bundled native SDK DLL or helper process.
- Shows CPU package power, CPU temperature sensors, per-core load/temperature/power rows, memory usage/temperature, GPU power/temperature/VRAM sensors, and storage usage/temperature/read-write sensors.
- Lists every exposed GPU and storage device instead of assuming a single card or disk.
- Keeps the first version intentionally small: no tray icon, no chart history, no alternate sensor backend.
- Requires administrator elevation so low-level hardware sensors can be opened consistently.

## Build

Install the .NET SDK and Windows App SDK WinUI workload, then open `RemoSystemProfiler.sln` in Visual Studio 2022 or newer and build the `x64` configuration.

The app targets `net8.0-windows10.0.19041.0`, Windows App SDK `1.8.*`, and `LibreHardwareMonitorLib`.

If Visual Studio opens the solution with the project unloaded, install the components listed in `.vsconfig` or use Visual Studio Installer > Modify > WinUI application development.

For Visual Studio F5 debugging, start Visual Studio itself with **Run as administrator**. The app manifest requests `requireAdministrator`, and packaged MSIX builds also declare the restricted `allowElevation` capability so published launches can show the UAC prompt.

For a quick command-line development build without MSIX packaging:

```powershell
dotnet build RemoSystemProfiler.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None
```

For MSIX project build validation on this machine's current Visual Studio install:

```powershell
$env:MSBuildSDKsPath = 'C:\Program Files\dotnet\sdk\8.0.420\Sdks'
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' RemoSystemProfiler.csproj /p:Configuration=Debug /p:Platform=x64 /p:MSBuildEnableWorkloadResolver=false
```
