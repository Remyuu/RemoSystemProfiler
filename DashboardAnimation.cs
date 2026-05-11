namespace RemoSystemProfiler;

internal static class DashboardAnimation
{
    public const int DurationMilliseconds = 180;
    public const int Frames = 12;

    public static double EaseOutCubic(double value)
    {
        double inverted = 1 - value;
        return 1 - inverted * inverted * inverted;
    }

    public static double Lerp(double start, double end, double amount) => start + (end - start) * amount;
}
