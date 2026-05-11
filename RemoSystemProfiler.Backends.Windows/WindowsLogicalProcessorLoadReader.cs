using System.Runtime.InteropServices;
using RemoSystemProfiler.Core;

namespace RemoSystemProfiler.Backends.Windows;

internal sealed class WindowsLogicalProcessorLoadReader
{
    private const int SystemProcessorPerformanceInformationClass = 8;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);

    private ProcessorTimes[]? _previous;

    public IReadOnlyList<CoreReading>? ReadLoadPercentages(int expectedLogicalProcessorCount)
    {
        ProcessorTimes[] current = ReadProcessorTimes(expectedLogicalProcessorCount);
        if (current.Length == 0)
        {
            return null;
        }

        if (_previous is null || _previous.Length != current.Length)
        {
            _previous = current;
            return null;
        }

        CoreReading[] readings = new CoreReading[current.Length];
        for (int index = 0; index < current.Length; index++)
        {
            long idle = current[index].IdleTime - _previous[index].IdleTime;
            long kernel = current[index].KernelTime - _previous[index].KernelTime;
            long user = current[index].UserTime - _previous[index].UserTime;
            long total = kernel + user;
            long busy = total - idle;
            int load = total <= 0
                ? 0
                : (int)Math.Round(Math.Clamp(busy * 100d / total, 0, 100));
            readings[index] = new CoreReading(index, load);
        }

        _previous = current;
        return readings;
    }

    private static ProcessorTimes[] ReadProcessorTimes(int expectedLogicalProcessorCount)
    {
        int expectedCount = Math.Max(1, expectedLogicalProcessorCount > 0 ? expectedLogicalProcessorCount : Environment.ProcessorCount);
        int itemSize = Marshal.SizeOf<SystemProcessorPerformanceInformation>();
        int bufferLength = itemSize * expectedCount;
        IntPtr buffer = Marshal.AllocHGlobal(bufferLength);
        try
        {
            int status = NtQuerySystemInformation(SystemProcessorPerformanceInformationClass, buffer, bufferLength, out int returnLength);
            if (status == StatusInfoLengthMismatch && returnLength > bufferLength)
            {
                Marshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
                bufferLength = returnLength;
                buffer = Marshal.AllocHGlobal(bufferLength);
                status = NtQuerySystemInformation(SystemProcessorPerformanceInformationClass, buffer, bufferLength, out returnLength);
            }

            if (status < 0)
            {
                return [];
            }

            int actualCount = returnLength > 0
                ? Math.Min(returnLength / itemSize, expectedCount)
                : expectedCount;
            if (actualCount <= 0)
            {
                return [];
            }

            ProcessorTimes[] result = new ProcessorTimes[actualCount];
            for (int index = 0; index < result.Length; index++)
            {
                IntPtr itemPtr = IntPtr.Add(buffer, index * itemSize);
                SystemProcessorPerformanceInformation item = Marshal.PtrToStructure<SystemProcessorPerformanceInformation>(itemPtr);
                result[index] = new ProcessorTimes(item.IdleTime, item.KernelTime, item.UserTime);
            }

            return result;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(
        int systemInformationClass,
        IntPtr systemInformation,
        int systemInformationLength,
        out int returnLength);

    private readonly record struct ProcessorTimes(long IdleTime, long KernelTime, long UserTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemProcessorPerformanceInformation
    {
        public long IdleTime;
        public long KernelTime;
        public long UserTime;
        public long DpcTime;
        public long InterruptTime;
        public uint InterruptCount;
    }
}
