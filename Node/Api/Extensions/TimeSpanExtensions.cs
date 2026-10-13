namespace Wookashi.FeatureSwitcher.Node.Api.Extensions;

internal static class TimeSpanExtensions
{
    /// <summary>
    /// Drops sub-second precision so durations read as e.g. <c>1.02:03:04</c> in logs.
    /// </summary>
    internal static TimeSpan TruncateToSeconds(this TimeSpan value) =>
        TimeSpan.FromSeconds(Math.Floor(value.TotalSeconds));
}
