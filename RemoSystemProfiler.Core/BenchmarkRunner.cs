using System.Diagnostics;
using System.IO.Hashing;
using System.Numerics;
using ZstdSharp;

namespace RemoSystemProfiler.Core;

public enum BenchmarkRunProfile
{
    Quick,
    Standard,
    Sustained
}

public enum BenchmarkScoreKind
{
    CpuCore,
    CpuMixed
}

public readonly record struct BenchmarkProfilePlan(
    BenchmarkRunProfile Profile,
    TimeSpan Warmup,
    TimeSpan SciMark,
    TimeSpan ZstdCompression,
    TimeSpan ZstdDecompression,
    TimeSpan Hash)
{
    public TimeSpan TotalDuration => Warmup + SciMark + ZstdCompression + ZstdDecompression + Hash;
}

public readonly record struct BenchmarkProgress(
    double Ratio,
    string StageName,
    TimeSpan Elapsed,
    TimeSpan Duration,
    BenchmarkWorkloadResult? CompletedWorkload = null,
    double? ZstdRatio = null);

public readonly record struct BenchmarkWorkloadResult(
    string Name,
    double Score,
    string ThroughputText,
    ulong Checksum,
    double ThroughputGbps = 0);

public readonly record struct BenchmarkResult(
    string Version,
    BenchmarkRunProfile Profile,
    double ElapsedSeconds,
    int WorkerCount,
    double Score,
    double CpuCoreScore,
    ulong Checksum,
    BenchmarkWorkloadResult SciMark,
    BenchmarkWorkloadResult ZstdCompression,
    BenchmarkWorkloadResult ZstdDecompression,
    double ZstdRatio,
    BenchmarkWorkloadResult Hash,
    bool IsValid)
{
    public double CpuMixedScore => Score;
}

public static class BenchmarkRunner
{
    public const string Version = "2.2";
    public const string LegacyVersion = "2.0";

    private const int SciMarkKernelCount = 5;
    private const int ZstdPayloadBytes = 1 * 1024 * 1024;
    private const int HashPayloadBytes = 8 * 1024 * 1024;
    private const double CurrentScoreTarget = 1000d;
    private const double CurrentSciMarkScoreReference = 420_000_000d;
    private const double CurrentZstdCompressionScoreReference = 50_000d;
    private const double CurrentZstdDecompressionScoreReference = 50_000d;
    private const double CurrentHashScoreReference = 50_000d;

    public static int MaxWorkerCount => Math.Max(1, Environment.ProcessorCount);

    public static int RecommendedWorkerCount => MaxWorkerCount;

    public static BenchmarkProfilePlan GetPlan(BenchmarkRunProfile profile) => GetPlan(profile, Version);

    public static BenchmarkProfilePlan GetPlan(BenchmarkRunProfile profile, string? benchmarkVersion)
    {
        return IsLegacyVersion(benchmarkVersion)
            ? GetLegacyPlan(profile)
            : GetCurrentPlan(profile);
    }

    public static bool IsSupportedVersion(string? benchmarkVersion)
    {
        string version = NormalizeVersion(benchmarkVersion);
        return string.Equals(version, Version, StringComparison.Ordinal)
            || string.Equals(version, LegacyVersion, StringComparison.Ordinal);
    }

    public static bool IsLegacyVersion(string? benchmarkVersion)
    {
        return string.Equals(NormalizeVersion(benchmarkVersion), LegacyVersion, StringComparison.Ordinal);
    }

    public static bool SupportsCpuCoreScore(string? benchmarkVersion) => !IsLegacyVersion(benchmarkVersion);

    public static string NormalizeVersion(string? benchmarkVersion)
    {
        return string.IsNullOrWhiteSpace(benchmarkVersion)
            ? Version
            : benchmarkVersion.Trim();
    }

    private static BenchmarkProfilePlan GetCurrentPlan(BenchmarkRunProfile profile) => profile switch
    {
        BenchmarkRunProfile.Quick => new(
            profile,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(12),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(4)),
        BenchmarkRunProfile.Sustained => new(
            profile,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(90),
            TimeSpan.FromSeconds(25),
            TimeSpan.FromSeconds(25),
            TimeSpan.FromSeconds(25)),
        _ => new(
            BenchmarkRunProfile.Standard,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(35),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromSeconds(8))
    };

    private static BenchmarkProfilePlan GetLegacyPlan(BenchmarkRunProfile profile) => profile switch
    {
        BenchmarkRunProfile.Quick => new(
            profile,
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(12),
            TimeSpan.FromSeconds(6),
            TimeSpan.FromSeconds(6),
            TimeSpan.FromSeconds(8)),
        BenchmarkRunProfile.Sustained => new(
            profile,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(40),
            TimeSpan.FromSeconds(75),
            TimeSpan.FromSeconds(45),
            TimeSpan.FromSeconds(25)),
        _ => new(
            BenchmarkRunProfile.Standard,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(18),
            TimeSpan.FromSeconds(12),
            TimeSpan.FromSeconds(12))
    };

    public static async Task<BenchmarkResult> RunAsync(
        BenchmarkRunProfile profile,
        int workerCount,
        IProgress<BenchmarkProgress>? progress,
        CancellationToken cancellationToken) => await RunAsync(Version, profile, workerCount, progress, cancellationToken).ConfigureAwait(false);

    public static async Task<BenchmarkResult> RunAsync(
        string? benchmarkVersion,
        BenchmarkRunProfile profile,
        int workerCount,
        IProgress<BenchmarkProgress>? progress,
        CancellationToken cancellationToken)
    {
        string version = NormalizeVersion(benchmarkVersion);
        BenchmarkProfilePlan plan = GetPlan(profile, version);
        workerCount = Math.Clamp(workerCount, 1, MaxWorkerCount);
        bool legacyScoring = IsLegacyVersion(version);

        long startedAt = Stopwatch.GetTimestamp();
        TimeSpan completed = TimeSpan.Zero;

        await RunWorkloadStageAsync(
            "Warmup",
            completed,
            plan.TotalDuration,
            plan.Warmup,
            workerCount,
            RunWarmupWorker,
            progress,
            startedAt,
            cancellationToken).ConfigureAwait(false);
        completed += plan.Warmup;

        WorkerResult sciMarkTotal = default;
        TimeSpan[] sciMarkDurations = SplitDuration(plan.SciMark, SciMarkKernelCount);
        for (int i = 0; i < sciMarkDurations.Length; i++)
        {
            SciMarkKernel kernel = (SciMarkKernel)i;
            WorkerResult result = await RunWorkloadStageAsync(
                $"SciMark {kernel}",
                completed,
                plan.TotalDuration,
                sciMarkDurations[i],
                workerCount,
                (workerIndex, deadline, token) => RunSciMarkWorker(workerIndex, kernel, deadline, token),
                progress,
                startedAt,
                cancellationToken).ConfigureAwait(false);
            sciMarkTotal = sciMarkTotal.Combine(result);
            completed += sciMarkDurations[i];
        }
        BenchmarkWorkloadResult sciMark = BuildSciMarkResult(sciMarkTotal, plan.SciMark);
        if (!legacyScoring)
        {
            sciMark = NormalizeCurrentWorkloadScore(sciMark, CurrentSciMarkScoreReference);
        }

        ReportCompletedWorkload(progress, startedAt, plan.TotalDuration, completed, sciMark);

        WorkerResult zstdCompressionResult = await RunWorkloadStageAsync(
            "zstd compress",
            completed,
            plan.TotalDuration,
            plan.ZstdCompression,
            workerCount,
            RunZstdCompressionWorker,
            progress,
            startedAt,
            cancellationToken).ConfigureAwait(false);
        completed += plan.ZstdCompression;
        BenchmarkWorkloadResult zstdCompression = BuildZstdCompressionResult(zstdCompressionResult, plan.ZstdCompression);
        if (!legacyScoring)
        {
            zstdCompression = NormalizeCurrentWorkloadScore(zstdCompression, CurrentZstdCompressionScoreReference);
        }

        double ratio = zstdCompressionResult.ExtraUnits <= 0 ? 0 : zstdCompressionResult.Units / zstdCompressionResult.ExtraUnits;
        ReportCompletedWorkload(progress, startedAt, plan.TotalDuration, completed, zstdCompression, legacyScoring ? ratio : null);

        WorkerResult zstdDecompressionResult = await RunWorkloadStageAsync(
            "zstd decompress",
            completed,
            plan.TotalDuration,
            plan.ZstdDecompression,
            workerCount,
            RunZstdDecompressionWorker,
            progress,
            startedAt,
            cancellationToken).ConfigureAwait(false);
        completed += plan.ZstdDecompression;
        BenchmarkWorkloadResult zstdDecompression = BuildZstdDecompressionResult(zstdDecompressionResult, plan.ZstdDecompression);
        if (!legacyScoring)
        {
            zstdDecompression = NormalizeCurrentWorkloadScore(zstdDecompression, CurrentZstdDecompressionScoreReference);
        }

        ReportCompletedWorkload(progress, startedAt, plan.TotalDuration, completed, zstdDecompression, legacyScoring ? ratio : null);

        WorkerResult hashResult = await RunWorkloadStageAsync(
            "XxHash3",
            completed,
            plan.TotalDuration,
            plan.Hash,
            workerCount,
            RunHashWorker,
            progress,
            startedAt,
            cancellationToken).ConfigureAwait(false);
        completed += plan.Hash;
        BenchmarkWorkloadResult hash = BuildHashResult(hashResult, plan.Hash);
        if (!legacyScoring)
        {
            hash = NormalizeCurrentWorkloadScore(hash, CurrentHashScoreReference);
        }

        ReportCompletedWorkload(progress, startedAt, plan.TotalDuration, completed, hash);

        double elapsedSeconds = Math.Max(0.001, (Stopwatch.GetTimestamp() - startedAt) / (double)Stopwatch.Frequency);
        double mixedScore = legacyScoring
            ? GeometricMean(sciMark.Score, zstdCompression.Score, zstdDecompression.Score, hash.Score)
            : WeightedGeometricMean(
                (sciMark.Score, 70d),
                (zstdCompression.Score, 10d),
                (zstdDecompression.Score, 10d),
                (hash.Score, 10d));
        double coreScore = legacyScoring ? mixedScore : sciMark.Score;
        ulong checksum = sciMark.Checksum
            ^ BitOperations.RotateLeft(zstdCompression.Checksum, 13)
            ^ BitOperations.RotateLeft(zstdDecompression.Checksum, 29)
            ^ BitOperations.RotateLeft(hash.Checksum, 47);

        progress?.Report(new BenchmarkProgress(1, "Complete", TimeSpan.FromSeconds(elapsedSeconds), plan.TotalDuration));

        bool isValid = IsValidResult(plan, elapsedSeconds, mixedScore, coreScore, sciMark, zstdCompression, zstdDecompression, ratio, hash, requireZstdRatio: legacyScoring);
        return new BenchmarkResult(version, plan.Profile, elapsedSeconds, workerCount, mixedScore, coreScore, checksum, sciMark, zstdCompression, zstdDecompression, legacyScoring ? ratio : 0, hash, isValid);
    }

    private static BenchmarkWorkloadResult BuildSciMarkResult(WorkerResult result, TimeSpan duration)
    {
        double unitsPerSecond = result.Units / Math.Max(0.001, duration.TotalSeconds);
        return new BenchmarkWorkloadResult("SciMark", unitsPerSecond * 100d, $"{unitsPerSecond:0.0} iter/s", result.Checksum);
    }

    private static BenchmarkWorkloadResult BuildZstdCompressionResult(WorkerResult result, TimeSpan duration)
    {
        double megabytesPerSecond = result.Units / Math.Max(0.001, duration.TotalSeconds);
        return new BenchmarkWorkloadResult(
            "zstd compression",
            megabytesPerSecond,
            FormatBytesPerSecond(megabytesPerSecond * 1024d * 1024d),
            result.Checksum,
            megabytesPerSecond / 1024d);
    }

    private static BenchmarkWorkloadResult BuildZstdDecompressionResult(WorkerResult result, TimeSpan duration)
    {
        double megabytesPerSecond = result.Units / Math.Max(0.001, duration.TotalSeconds);
        return new BenchmarkWorkloadResult(
            "zstd decompression",
            megabytesPerSecond,
            FormatBytesPerSecond(megabytesPerSecond * 1024d * 1024d),
            result.Checksum,
            megabytesPerSecond / 1024d);
    }

    private static BenchmarkWorkloadResult BuildHashResult(WorkerResult result, TimeSpan duration)
    {
        double gigabytesPerSecond = result.Units / Math.Max(0.001, duration.TotalSeconds);
        return new BenchmarkWorkloadResult(
            "XxHash3",
            gigabytesPerSecond * 50d,
            FormatBytesPerSecond(gigabytesPerSecond * 1024d * 1024d * 1024d),
            result.Checksum,
            gigabytesPerSecond);
    }

    private static BenchmarkWorkloadResult NormalizeCurrentWorkloadScore(BenchmarkWorkloadResult result, double referenceScore)
    {
        double normalized = CurrentScoreTarget * Math.Sqrt(Math.Max(0, result.Score) / Math.Max(1, referenceScore));
        return result with { Score = normalized };
    }

    private static bool IsValidResult(
        BenchmarkProfilePlan plan,
        double elapsedSeconds,
        double score,
        double coreScore,
        BenchmarkWorkloadResult sciMark,
        BenchmarkWorkloadResult zstdCompression,
        BenchmarkWorkloadResult zstdDecompression,
        double zstdRatio,
        BenchmarkWorkloadResult hash,
        bool requireZstdRatio)
    {
        return elapsedSeconds >= plan.TotalDuration.TotalSeconds * 0.85
            && IsFinitePositive(score)
            && IsFinitePositive(coreScore)
            && IsFinitePositive(sciMark.Score)
            && IsFinitePositive(zstdCompression.Score)
            && IsFinitePositive(zstdDecompression.Score)
            && IsFinitePositive(zstdCompression.ThroughputGbps)
            && IsFinitePositive(zstdDecompression.ThroughputGbps)
            && (!requireZstdRatio || IsFinitePositive(zstdRatio))
            && IsFinitePositive(hash.Score)
            && IsFinitePositive(hash.ThroughputGbps);
    }

    private static bool IsFinitePositive(double value)
    {
        return double.IsFinite(value) && value > 0;
    }

    private static void ReportCompletedWorkload(
        IProgress<BenchmarkProgress>? progress,
        long benchmarkStartedAt,
        TimeSpan totalDuration,
        TimeSpan completed,
        BenchmarkWorkloadResult workload,
        double? zstdRatio = null)
    {
        if (progress is null)
        {
            return;
        }

        TimeSpan elapsed = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - benchmarkStartedAt) / (double)Stopwatch.Frequency);
        double ratio = Math.Clamp(completed.TotalSeconds / totalDuration.TotalSeconds, 0, 1);
        progress.Report(new BenchmarkProgress(ratio, $"{workload.Name} complete", elapsed, totalDuration, workload, zstdRatio));
    }

    private static async Task<WorkerResult> RunWorkloadStageAsync(
        string name,
        TimeSpan completedBefore,
        TimeSpan totalDuration,
        TimeSpan stageDuration,
        int workerCount,
        Func<int, long, CancellationToken, WorkerResult> worker,
        IProgress<BenchmarkProgress>? progress,
        long benchmarkStartedAt,
        CancellationToken cancellationToken)
    {
        long startedAt = Stopwatch.GetTimestamp();
        long durationTicks = Math.Max(1, (long)Math.Round(stageDuration.TotalSeconds * Stopwatch.Frequency));
        long deadline = startedAt + durationTicks;

        using CancellationTokenSource progressStop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task progressTask = ReportProgressAsync(name, completedBefore, stageDuration, totalDuration, benchmarkStartedAt, startedAt, deadline, progress, progressStop.Token);

        Task<WorkerResult>[] workers = new Task<WorkerResult>[workerCount];
        for (int i = 0; i < workers.Length; i++)
        {
            int workerIndex = i;
            workers[i] = Task.Factory.StartNew(
                () => worker(workerIndex, deadline, cancellationToken),
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

        WorkerResult total = default;
        for (int i = 0; i < results.Length; i++)
        {
            total = total.Combine(results[i].RotateChecksum(i));
        }

        return total;
    }

    private static async Task ReportProgressAsync(
        string stageName,
        TimeSpan completedBefore,
        TimeSpan stageDuration,
        TimeSpan totalDuration,
        long benchmarkStartedAt,
        long stageStartedAt,
        long deadline,
        IProgress<BenchmarkProgress>? progress,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            long now = Stopwatch.GetTimestamp();
            double stageRatio = Math.Clamp((now - stageStartedAt) / (double)(deadline - stageStartedAt), 0, 1);
            double totalRatio = Math.Clamp((completedBefore.TotalSeconds + stageRatio * stageDuration.TotalSeconds) / totalDuration.TotalSeconds, 0, 1);
            TimeSpan elapsed = TimeSpan.FromSeconds((now - benchmarkStartedAt) / (double)Stopwatch.Frequency);
            progress?.Report(new BenchmarkProgress(totalRatio, stageName, elapsed, totalDuration));

            if (stageRatio >= 1)
            {
                return;
            }

            await Task.Delay(160, cancellationToken).ConfigureAwait(false);
        }
    }

    private static WorkerResult RunWarmupWorker(int workerIndex, long deadline, CancellationToken cancellationToken)
    {
        SciMarkState sciMark = new(workerIndex + 101);
        byte[] payload = CreatePayload(256 * 1024, workerIndex + 201);
        ulong checksum = 0;
        double iterations = 0;

        using Compressor compressor = new(level: 1);
        using Decompressor decompressor = new();
        while (Stopwatch.GetTimestamp() < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            checksum ^= sciMark.RunKernel(SciMarkKernel.Fft);
            byte[] compressed = compressor.Wrap(payload).ToArray();
            byte[] decompressed = decompressor.Unwrap(compressed).ToArray();
            checksum ^= XxHash3.HashToUInt64(decompressed);
            checksum ^= XxHash3.HashToUInt64(payload, workerIndex + (long)iterations);
            iterations++;
        }

        return new WorkerResult(iterations, checksum);
    }

    private static WorkerResult RunSciMarkWorker(int workerIndex, SciMarkKernel kernel, long deadline, CancellationToken cancellationToken)
    {
        Thread currentThread = Thread.CurrentThread;
        ThreadPriority originalPriority = currentThread.Priority;

        SciMarkState state = new(workerIndex + (int)kernel * 37 + 1);
        double iterations = 0;
        ulong checksum = 0;

        try
        {
            currentThread.Priority = ThreadPriority.BelowNormal;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                checksum ^= state.RunKernel(kernel);
                iterations++;
            }
        }
        finally
        {
            currentThread.Priority = originalPriority;
        }

        return new WorkerResult(iterations, checksum);
    }

    private static WorkerResult RunZstdCompressionWorker(int workerIndex, long deadline, CancellationToken cancellationToken)
    {
        Thread currentThread = Thread.CurrentThread;
        ThreadPriority originalPriority = currentThread.Priority;

        byte[] payload = CreatePayload(ZstdPayloadBytes, workerIndex + 11);
        double inputMegabytes = 0;
        double compressedMegabytes = 0;
        ulong checksum = XxHash3.HashToUInt64(payload);

        try
        {
            currentThread.Priority = ThreadPriority.BelowNormal;
            using Compressor compressor = new(level: 3);
            while (Stopwatch.GetTimestamp() < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte[] compressed = compressor.Wrap(payload).ToArray();
                inputMegabytes += payload.Length / (1024d * 1024d);
                compressedMegabytes += compressed.Length / (1024d * 1024d);
                checksum ^= XxHash3.HashToUInt64(compressed);
            }
        }
        finally
        {
            currentThread.Priority = originalPriority;
        }

        return new WorkerResult(inputMegabytes, checksum, compressedMegabytes);
    }

    private static WorkerResult RunZstdDecompressionWorker(int workerIndex, long deadline, CancellationToken cancellationToken)
    {
        Thread currentThread = Thread.CurrentThread;
        ThreadPriority originalPriority = currentThread.Priority;

        byte[] payload = CreatePayload(ZstdPayloadBytes, workerIndex + 11);
        ulong expectedHash = XxHash3.HashToUInt64(payload);
        double outputMegabytes = 0;
        ulong checksum = expectedHash;

        try
        {
            currentThread.Priority = ThreadPriority.BelowNormal;
            using Compressor compressor = new(level: 3);
            using Decompressor decompressor = new();
            byte[] compressed = compressor.Wrap(payload).ToArray();

            while (Stopwatch.GetTimestamp() < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte[] decompressed = decompressor.Unwrap(compressed).ToArray();
                ulong actualHash = XxHash3.HashToUInt64(decompressed);
                if (actualHash != expectedHash || decompressed.Length != payload.Length)
                {
                    throw new InvalidOperationException("zstd verification failed.");
                }

                outputMegabytes += decompressed.Length / (1024d * 1024d);
                checksum ^= actualHash;
            }
        }
        finally
        {
            currentThread.Priority = originalPriority;
        }

        return new WorkerResult(outputMegabytes, checksum);
    }

    private static WorkerResult RunHashWorker(int workerIndex, long deadline, CancellationToken cancellationToken)
    {
        Thread currentThread = Thread.CurrentThread;
        ThreadPriority originalPriority = currentThread.Priority;

        byte[] payload = CreatePayload(HashPayloadBytes, workerIndex + 31);
        double gigabytes = 0;
        ulong checksum = 0;
        long seed = workerIndex;

        try
        {
            currentThread.Priority = ThreadPriority.BelowNormal;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                checksum ^= XxHash3.HashToUInt64(payload, seed++);
                gigabytes += payload.Length / (1024d * 1024d * 1024d);
            }
        }
        finally
        {
            currentThread.Priority = originalPriority;
        }

        return new WorkerResult(gigabytes, checksum);
    }

    private static byte[] CreatePayload(int length, int seed)
    {
        byte[] payload = new byte[length];
        uint state = (uint)(0x9E3779B9u ^ seed);
        for (int i = 0; i < payload.Length; i++)
        {
            if ((i & 4095) == 0)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
            }

            byte pattern = (byte)((i * 31 + seed) & 0xFF);
            byte noise = (byte)(state >> ((i & 3) * 8));
            payload[i] = (byte)(pattern ^ ((i & 63) == 0 ? noise : 0));
        }

        return payload;
    }

    private static TimeSpan[] SplitDuration(TimeSpan duration, int count)
    {
        TimeSpan[] durations = new TimeSpan[count];
        long baseTicks = duration.Ticks / count;
        long assignedTicks = 0;
        for (int i = 0; i < count; i++)
        {
            long ticks = i == count - 1 ? duration.Ticks - assignedTicks : baseTicks;
            durations[i] = TimeSpan.FromTicks(Math.Max(TimeSpan.TicksPerMillisecond, ticks));
            assignedTicks += ticks;
        }

        return durations;
    }

    private static double GeometricMean(params double[] values)
    {
        double product = 1;
        foreach (double value in values)
        {
            product *= Math.Max(1, value);
        }

        return Math.Pow(product, 1d / values.Length);
    }

    private static double WeightedGeometricMean(params (double Value, double Weight)[] values)
    {
        double totalWeight = 0;
        foreach ((_, double weight) in values)
        {
            totalWeight += Math.Max(0, weight);
        }

        if (totalWeight <= 0)
        {
            return GeometricMean(values.Select(value => value.Value).ToArray());
        }

        double weightedLog = 0;
        foreach ((double value, double weight) in values)
        {
            weightedLog += Math.Max(0, weight) / totalWeight * Math.Log(Math.Max(1, value));
        }

        return Math.Exp(weightedLog);
    }

    private static string FormatBytesPerSecond(double bytesPerSecond)
    {
        return bytesPerSecond switch
        {
            >= 1024d * 1024d * 1024d => $"{bytesPerSecond / (1024d * 1024d * 1024d):0.00} GB/s",
            >= 1024d * 1024d => $"{bytesPerSecond / (1024d * 1024d):0.0} MB/s",
            >= 1024d => $"{bytesPerSecond / 1024d:0.0} KB/s",
            _ => $"{bytesPerSecond:0} B/s"
        };
    }

    private readonly record struct WorkerResult(double Units, ulong Checksum, double ExtraUnits = 0)
    {
        public WorkerResult Combine(WorkerResult other) => new(Units + other.Units, Checksum ^ other.Checksum, ExtraUnits + other.ExtraUnits);

        public WorkerResult RotateChecksum(int offset) => new(Units, BitOperations.RotateLeft(Checksum, offset % 64), ExtraUnits);
    }

    private enum SciMarkKernel
    {
        Fft,
        Sor,
        Lu,
        Sparse,
        MonteCarlo
    }

    private sealed class SciMarkState
    {
        private const int FftSize = 256;
        private const int SorSize = 32;
        private const int LuSize = 24;
        private const int SparseSize = 128;
        private const int SparseNonZero = 1024;

        private readonly double[] _real = new double[FftSize];
        private readonly double[] _imag = new double[FftSize];
        private readonly double[,] _grid = new double[SorSize, SorSize];
        private readonly double[,] _lu = new double[LuSize, LuSize];
        private readonly double[] _sparseX = new double[SparseSize];
        private readonly double[] _sparseY = new double[SparseSize];
        private readonly int[] _sparseRows = new int[SparseNonZero];
        private readonly int[] _sparseColumns = new int[SparseNonZero];
        private readonly double[] _sparseValues = new double[SparseNonZero];
        private ulong _randomState;

        public SciMarkState(int seed)
        {
            _randomState = 0xD1B54A32D192ED03UL ^ (uint)seed;

            for (int i = 0; i < FftSize; i++)
            {
                _real[i] = NextDouble();
                _imag[i] = NextDouble() * 0.5;
            }

            for (int y = 0; y < SorSize; y++)
            {
                for (int x = 0; x < SorSize; x++)
                {
                    _grid[y, x] = NextDouble();
                }
            }

            for (int y = 0; y < LuSize; y++)
            {
                for (int x = 0; x < LuSize; x++)
                {
                    _lu[y, x] = (y == x ? 4d : 0d) + NextDouble();
                }
            }

            for (int i = 0; i < SparseSize; i++)
            {
                _sparseX[i] = NextDouble();
            }

            for (int i = 0; i < SparseNonZero; i++)
            {
                _sparseRows[i] = i % SparseSize;
                _sparseColumns[i] = (i * 17 + seed) % SparseSize;
                _sparseValues[i] = NextDouble() - 0.5;
            }
        }

        public ulong RunKernel(SciMarkKernel kernel)
        {
            double checksum = kernel switch
            {
                SciMarkKernel.Fft => RunFft(),
                SciMarkKernel.Sor => RunSor(),
                SciMarkKernel.Lu => RunLu(),
                SciMarkKernel.Sparse => RunSparseMatMult(),
                _ => RunMonteCarlo()
            };

            return (ulong)BitConverter.DoubleToInt64Bits(checksum);
        }

        private double RunFft()
        {
            for (int length = 2; length <= FftSize; length <<= 1)
            {
                double angle = -2d * Math.PI / length;
                double wLenReal = Math.Cos(angle);
                double wLenImag = Math.Sin(angle);

                for (int i = 0; i < FftSize; i += length)
                {
                    double wReal = 1;
                    double wImag = 0;
                    int half = length >> 1;
                    for (int j = 0; j < half; j++)
                    {
                        int even = i + j;
                        int odd = even + half;
                        double oddReal = _real[odd] * wReal - _imag[odd] * wImag;
                        double oddImag = _real[odd] * wImag + _imag[odd] * wReal;

                        _real[odd] = _real[even] - oddReal;
                        _imag[odd] = _imag[even] - oddImag;
                        _real[even] += oddReal;
                        _imag[even] += oddImag;

                        double nextReal = wReal * wLenReal - wImag * wLenImag;
                        wImag = wReal * wLenImag + wImag * wLenReal;
                        wReal = nextReal;
                    }
                }
            }

            const double scale = 0.996 / 16d;
            for (int i = 0; i < FftSize; i++)
            {
                _real[i] *= scale;
                _imag[i] *= scale;
            }

            return _real[3] + _imag[5];
        }

        private double RunSor()
        {
            for (int sweep = 0; sweep < 6; sweep++)
            {
                for (int y = 1; y < SorSize - 1; y++)
                {
                    for (int x = 1; x < SorSize - 1; x++)
                    {
                        double average = (_grid[y - 1, x] + _grid[y + 1, x] + _grid[y, x - 1] + _grid[y, x + 1]) * 0.25;
                        _grid[y, x] += 1.25 * (average - _grid[y, x]);
                    }
                }
            }

            return _grid[7, 11];
        }

        private double RunSparseMatMult()
        {
            Array.Clear(_sparseY);
            for (int repeat = 0; repeat < 4; repeat++)
            {
                for (int i = 0; i < SparseNonZero; i++)
                {
                    _sparseY[_sparseRows[i]] += _sparseValues[i] * _sparseX[_sparseColumns[i]];
                }
            }

            (_sparseX[0], _sparseX[1]) = (_sparseY[1], _sparseY[0]);
            return _sparseY[13];
        }

        private double RunLu()
        {
            double trace = 0;
            for (int k = 0; k < LuSize - 1; k++)
            {
                double pivot = Math.Abs(_lu[k, k]) < 0.001 ? 0.001 : _lu[k, k];
                for (int i = k + 1; i < LuSize; i++)
                {
                    double factor = _lu[i, k] / pivot;
                    for (int j = k + 1; j < LuSize; j++)
                    {
                        _lu[i, j] -= factor * _lu[k, j];
                    }
                }

                trace += pivot;
            }

            for (int i = 0; i < LuSize; i++)
            {
                _lu[i, i] = 4d + Math.Abs(_lu[i, i] % 1d);
            }

            return trace;
        }

        private double RunMonteCarlo()
        {
            int inside = 0;
            for (int i = 0; i < 2048; i++)
            {
                double x = NextDouble();
                double y = NextDouble();
                if (x * x + y * y <= 1d)
                {
                    inside++;
                }
            }

            return inside / 512d;
        }

        private double NextDouble()
        {
            _randomState ^= _randomState << 7;
            _randomState ^= _randomState >> 9;
            _randomState *= 0xD6E8FEB86659FD93UL;
            return (_randomState >> 11) * (1d / (1UL << 53));
        }
    }
}
