using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;
using Wookashi.FeatureSwitcher.Node.Api.Extensions;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

/// <summary>
/// Logs the runtime/GC environment once at startup and then memory statistics at a fixed interval.
/// An OutOfMemory termination leaves no stack trace or heap information behind, so this trail is what
/// makes it possible to tell afterwards whether memory grew steadily (a leak) or suddenly, and whether
/// the growth was in the managed GC heap or in native memory (e.g. SQLite).
/// </summary>
internal sealed class RuntimeDiagnosticsHostedService(
    IOptions<NodeConfiguration> options,
    ILogger<RuntimeDiagnosticsHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Share of the GC memory limit above which a periodic entry is logged as a warning.
    /// </summary>
    private const double HighUsageRatio = 0.8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogEnvironment();

        var interval = options.Value.DiagnosticsInterval;
        if (interval <= TimeSpan.Zero)
        {
            return;
        }

        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    LogMemoryStatistics();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to collect memory statistics");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private void LogEnvironment()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var gcConfiguration = string.Join(", ",
            GC.GetConfigurationVariables().OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));

        logger.LogInformation(
            "Runtime: {Framework} on {OS}, {ProcessorCount} CPU(s) available, GC mode {GcMode} (latency {LatencyMode}), GC memory limit {MemoryLimitMb} MB; periodic memory statistics: {Statistics}",
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            Environment.ProcessorCount,
            GCSettings.IsServerGC ? "Server" : "Workstation",
            GCSettings.LatencyMode,
            ToMb(gcInfo.TotalAvailableMemoryBytes),
            options.Value.DiagnosticsInterval > TimeSpan.Zero
                ? $"every {options.Value.DiagnosticsInterval}"
                : "disabled (set NodeConfiguration__DiagnosticsInterval to enable)");
        logger.LogInformation("GC configuration: {GcConfiguration}", gcConfiguration);
    }

    private void LogMemoryStatistics()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        using var process = Process.GetCurrentProcess();

        var workingSet = process.WorkingSet64;
        var committed = gcInfo.TotalCommittedBytes;
        var limit = gcInfo.TotalAvailableMemoryBytes;
        // Rough estimate: everything resident that the GC did not commit — native libraries,
        // SQLite page caches, thread stacks, JIT-ed code.
        var nonGcEstimate = Math.Max(0, workingSet - committed);

        var isHigh = (limit > 0 && workingSet >= limit * HighUsageRatio)
                     || gcInfo.MemoryLoadBytes >= gcInfo.HighMemoryLoadThresholdBytes;

        logger.Log(
            isHigh ? LogLevel.Warning : LogLevel.Information,
            "Memory: working set {WorkingSetMb} MB (GC memory limit {MemoryLimitMb} MB); GC heap {HeapMb} MB (committed {CommittedMb} MB, fragmented {FragmentedMb} MB, managed allocated {AllocatedMb} MB); non-GC estimate {NonGcMb} MB; " +
            "GC count gen0/gen1/gen2 {Gen0}/{Gen1}/{Gen2}; threads {Threads} (thread pool {ThreadPoolThreads}, queued {QueuedWorkItems}); uptime {Uptime}",
            ToMb(workingSet),
            ToMb(limit),
            ToMb(gcInfo.HeapSizeBytes),
            ToMb(committed),
            ToMb(gcInfo.FragmentedBytes),
            ToMb(GC.GetTotalMemory(forceFullCollection: false)),
            ToMb(nonGcEstimate),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            process.Threads.Count,
            ThreadPool.ThreadCount,
            ThreadPool.PendingWorkItemCount,
            (DateTime.Now - process.StartTime).TruncateToSeconds());
    }

    private static long ToMb(long bytes) => bytes / (1024 * 1024);
}
