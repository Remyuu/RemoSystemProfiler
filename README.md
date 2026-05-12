# Remo System Profiler

Remo System Profiler is a small Windows app for checking your PC’s hardware status in real time. It shows CPU, memory, GPU, storage, temperature, load, power, and sensor info in one compact window.

Remo System Profiler 是一个小型 Windows 硬件监视工具，用来实时查看当前电脑的状态。它会把 CPU、内存、GPU、硬盘、温度、负载、功耗和传感器信息放在一个紧凑的窗口里。

The app is built with Avalonia and .NET 8. Hardware data on Windows is read through a lightweight backend based on LibreHardwareMonitor. The UI mainly focuses on showing the dashboard clearly.

这个应用使用 Avalonia 和 .NET 8 开发。Windows 上的硬件数据由一个轻量后端读取，底层用的是 LibreHardwareMonitor。界面部分主要负责把信息清楚地展示出来。

## What It Shows

The main page is split into several parts: system overview, CPU info and sensors, memory usage, GPU devices, storage devices, and a small CPU benchmark section. It also has simple settings for update speed, chart range, theme, language, and app info.

主界面分成几个区域：系统概览、CPU 信息和传感器、内存占用、GPU 设备、存储设备，还有一个小型 CPU 跑分区域。里面也有一些简单设置，比如刷新间隔、图表范围、主题、语言和关于信息。

Not every sensor is always available. It depends on your hardware, drivers, and permissions. The app can run without administrator rights, but some motherboard, fan, voltage, or temperature data may only show up after installing ==PawnIO== and restarting ==as administrator==.

不是所有传感器都一定能读到，这取决于你的硬件、驱动和权限。应用不强制管理员启动，但有些主板、风扇、电压或温度数据，可能需要安装 ==PawnIO==，并用==管理员身份==重启后才会显示。

## Use It

Open the solution with Visual Studio 2022 or newer, choose `x64`, and run the `RemoSystemProfiler` project. For local development, just run the normal unpackaged desktop version.

用 Visual Studio 2022 或更新版本打开解决方案，选择 `x64`，然后运行 `RemoSystemProfiler` 项目。日常本地开发时，直接用非打包的桌面运行方式就可以。

For a quick command-line build:

命令行快速构建可以用：

```powershell
dotnet build RemoSystemProfiler.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None
```

If the dashboard says sensor access is limited, check the message shown in the app first. Installing PawnIO or restarting as administrator may unlock more sensors. Even without full low-level access, storage info and many basic system readings should still work.

如果界面提示传感器访问受限，先看应用里的提示信息。安装 PawnIO 或用管理员身份重启，可能会解锁更多传感器。即使没有完整的底层权限，存储信息和很多基础系统数据通常也能正常显示。

## Project Shape

The project is split into three main parts: the Avalonia desktop app, shared profiler contracts in `RemoSystemProfiler.Core`, and the Windows sensor backend in `RemoSystemProfiler.Backends.Windows`. This keeps the current app simple, while still making it easier to replace the frontend later.

项目主要分成三部分：Avalonia 桌面应用、`RemoSystemProfiler.Core` 里的共享监视器接口，以及 `RemoSystemProfiler.Backends.Windows` 里的 Windows 传感器后端。这样现在的结构比较简单，以后如果想换前端也更方便。

Dashboard settings, such as chart range, update interval, theme, and language, are saved in a small local settings file next to the built app.

图表范围、刷新间隔、主题和语言这些设置，会保存在构建输出目录旁边的一个小型本地设置文件里。
