using System.Diagnostics;
using System.Numerics;

namespace RemoSystemProfiler.Core;

public readonly record struct CpuBenchmarkProgress(
    double Ratio,
    TimeSpan Elapsed,
    TimeSpan Duration);

public readonly record struct CpuBenchmarkResult(
    long Operations,
    double ElapsedSeconds,
    int WorkerCount,
    double Score,
    ulong Checksum)
{
    public double OperationsPerSecond => ElapsedSeconds <= 0 ? 0 : Operations / ElapsedSeconds;
}

public static class CpuBenchmarkRunner
{
    public const int DefaultDurationSeconds = 6;

    private const int OperationsPerBatch = 256;

    public static int MaxWorkerCount => Math.Max(1, Environment.ProcessorCount);

    public static int RecommendedWorkerCount => MaxWorkerCount;

    public static async Task<CpuBenchmarkResult> RunAsync(
        TimeSpan duration,
        int workerCount,
        IProgress<CpuBenchmarkProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Benchmark duration must be positive.");
        }

        int maxWorkers = MaxWorkerCount;
        workerCount = Math.Clamp(workerCount, 1, maxWorkers);

        long startedAt = Stopwatch.GetTimestamp();
        long durationTicks = Math.Max(1, (long)Math.Round(duration.TotalSeconds * Stopwatch.Frequency));
        long deadline = startedAt + durationTicks;

        using CancellationTokenSource progressStop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task progressTask = ReportProgressAsync(startedAt, deadline, duration, progress, progressStop.Token);

        Task<WorkerResult>[] workers = new Task<WorkerResult>[workerCount];
        for (int i = 0; i < workers.Length; i++)
        {
            int workerIndex = i;
            workers[i] = Task.Factory.StartNew(
                () => RunWorker(workerIndex, deadline, cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        WorkerResult[] results;
        try
        {
            results = await Task.WhenAll(workers).ConfigureAwait(false);
        }
        finally
        {
            progressStop.Cancel();
            try
            {
                await progressTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The benchmark work completed or was canceled; the progress loop is no longer needed.
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        long finishedAt = Stopwatch.GetTimestamp();
        double elapsedSeconds = Math.Max(0.001, (finishedAt - startedAt) / (double)Stopwatch.Frequency);
        long operations = 0;
        ulong checksum = 0;
        for (int i = 0; i < results.Length; i++)
        {
            operations += results[i].Operations;
            checksum ^= BitOperations.RotateLeft(results[i].Checksum, i % 64);
        }

        double operationsPerSecond = operations / elapsedSeconds;
        double score = operationsPerSecond / 100_000d;
        progress?.Report(new CpuBenchmarkProgress(1, TimeSpan.FromSeconds(elapsedSeconds), duration));

        return new CpuBenchmarkResult(operations, elapsedSeconds, workerCount, score, checksum);
    }

    private static async Task ReportProgressAsync(
        long startedAt,
        long deadline,
        TimeSpan duration,
        IProgress<CpuBenchmarkProgress>? progress,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            long now = Stopwatch.GetTimestamp();
            double ratio = Math.Clamp((now - startedAt) / (double)(deadline - startedAt), 0, 1);
            progress?.Report(new CpuBenchmarkProgress(ratio, TimeSpan.FromSeconds((now - startedAt) / (double)Stopwatch.Frequency), duration));

            if (ratio >= 1)
            {
                return;
            }

            await Task.Delay(160, cancellationToken).ConfigureAwait(false);
        }
    }

    private static WorkerResult RunWorker(int workerIndex, long deadline, CancellationToken cancellationToken)
    {
        Thread currentThread = Thread.CurrentThread;
        ThreadPriority originalPriority = currentThread.Priority;
        long operations = 0;
        ulong state = 0x9E3779B97F4A7C15UL ^ (ulong)(workerIndex + 1);

        try
        {
            currentThread.Priority = ThreadPriority.BelowNormal;

            while (Stopwatch.GetTimestamp() < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                unchecked
                {
                    for (int i = 0; i < OperationsPerBatch; i++)
                    {
                        state ^= state << 7;
                        state ^= state >> 9;
                        state *= 0xD6E8FEB86659FD93UL;
                        state = BitOperations.RotateLeft(state, 17);
                    }
                }

                operations += OperationsPerBatch;
            }

            return new WorkerResult(operations, state);
        }
        finally
        {
            currentThread.Priority = originalPriority;
        }
    }

    private readonly record struct WorkerResult(long Operations, ulong Checksum);
}
