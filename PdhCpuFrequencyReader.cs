using System.Runtime.InteropServices;

namespace RemoSystemProfiler;

public sealed class PdhCpuFrequencyReader : IDisposable
{
    private const uint ErrorSuccess = 0;
    private const uint PdhFmtDouble = 0x00000200;
    private const string PerformanceCounterPath = @"\Processor Information(_Total)\% Processor Performance";
    private const string FrequencyCounterPath = @"\Processor Information(_Total)\Processor Frequency";

    private IntPtr _query;
    private IntPtr _performanceCounter;
    private IntPtr _frequencyCounter;
    private bool _initialized;
    private bool _disposed;

    public float? ReadEffectiveClockMHz()
    {
        if (_disposed || !EnsureInitialized())
        {
            return null;
        }

        if (PdhCollectQueryData(_query) != ErrorSuccess)
        {
            return null;
        }

        double? baseFrequency = ReadCounter(_frequencyCounter);
        double? performance = ReadCounter(_performanceCounter);
        double? effective = baseFrequency > 0 && performance > 0
            ? baseFrequency * performance / 100d
            : baseFrequency;

        if (effective is null or <= 0 or > 10000)
        {
            return null;
        }

        return (float)effective.Value;
    }

    public void Dispose()
    {
        _disposed = true;
        if (_query != IntPtr.Zero)
        {
            PdhCloseQuery(_query);
            _query = IntPtr.Zero;
        }
    }

    private bool EnsureInitialized()
    {
        if (_initialized)
        {
            return _query != IntPtr.Zero;
        }

        _initialized = true;
        if (PdhOpenQuery(null, IntPtr.Zero, out _query) != ErrorSuccess)
        {
            _query = IntPtr.Zero;
            return false;
        }

        if (PdhAddEnglishCounter(_query, PerformanceCounterPath, IntPtr.Zero, out _performanceCounter) != ErrorSuccess
            || PdhAddEnglishCounter(_query, FrequencyCounterPath, IntPtr.Zero, out _frequencyCounter) != ErrorSuccess)
        {
            Dispose();
            return false;
        }

        _ = PdhCollectQueryData(_query);
        return true;
    }

    private static double? ReadCounter(IntPtr counter)
    {
        if (counter == IntPtr.Zero)
        {
            return null;
        }

        uint status = PdhGetFormattedCounterValue(counter, PdhFmtDouble, out _, out PdhFmtCounterValue value);
        return status == ErrorSuccess && value.CStatus == ErrorSuccess ? value.DoubleValue : null;
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhOpenQuery(string? dataSource, IntPtr userData, out IntPtr query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhAddEnglishCounter(IntPtr query, string fullCounterPath, IntPtr userData, out IntPtr counter);

    [DllImport("pdh.dll")]
    private static extern uint PdhCollectQueryData(IntPtr query);

    [DllImport("pdh.dll")]
    private static extern uint PdhGetFormattedCounterValue(IntPtr counter, uint format, out uint type, out PdhFmtCounterValue value);

    [DllImport("pdh.dll")]
    private static extern uint PdhCloseQuery(IntPtr query);

    [StructLayout(LayoutKind.Sequential)]
    private struct PdhFmtCounterValue
    {
        public uint CStatus;
        public double DoubleValue;
    }
}
