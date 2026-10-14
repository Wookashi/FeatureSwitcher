namespace Wookashi.FeatureSwitcher.Manager.Api.Configuration;

public sealed class FeatureUsageSettings
{
    public static readonly TimeSpan DefaultUnreadWarningAfter = TimeSpan.FromDays(45);

    /// <summary>
    /// After how long without reads a registered feature is reported as unread in the UI (dimmed cell,
    /// "Not read" filter, cleanup hint). Features registered more recently than this are not reported.
    /// Zero or negative disables the warning. Nodes derive the last read from their daily read counts,
    /// so their <c>UsageRetention</c> should be at least this long.
    /// </summary>
    public TimeSpan UnreadWarningAfter { get; set; } = DefaultUnreadWarningAfter;
}
