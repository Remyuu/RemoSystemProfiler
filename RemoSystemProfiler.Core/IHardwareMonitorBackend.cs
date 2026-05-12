namespace RemoSystemProfiler.Core;

public interface IHardwareMonitorBackend : IDisposable
{
    string Name { get; }

    HardwareMonitorReadResult Read();
}
