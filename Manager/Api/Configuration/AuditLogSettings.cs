namespace Wookashi.FeatureSwitcher.Manager.Api.Configuration;

public sealed class AuditLogSettings
{
    public static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(365);

    /// <summary>
    /// How long audit log entries are kept. Older entries are deleted once a day.
    /// Zero or negative disables the cleanup (entries are kept indefinitely).
    /// </summary>
    public TimeSpan Retention { get; set; } = DefaultRetention;
}
