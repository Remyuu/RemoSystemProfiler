namespace RemoSystemProfiler.Core;

public readonly record struct HardwareMonitorReadResult(
    bool IsAvailable,
    SystemSnapshot? Snapshot,
    string Message,
    bool RequiresAdministrator,
    SensorDriverStatus DriverStatus)
{
    public static HardwareMonitorReadResult Available(SystemSnapshot snapshot, bool requiresAdministrator) =>
        new(
            true,
            snapshot,
            requiresAdministrator ? "Elevated permissions may be required for full hardware sensors" : snapshot.DriverStatus.SummaryText,
            requiresAdministrator,
            snapshot.DriverStatus);

    public static HardwareMonitorReadResult Unavailable(string message, SensorDriverStatus driverStatus) =>
        new(false, null, message, false, driverStatus);
}
