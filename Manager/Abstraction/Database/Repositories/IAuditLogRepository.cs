using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

public interface IAuditLogRepository
{
    void AddEntry(string username, string action, string? details);

    /// <summary>
    /// Returns audit log entries ordered by timestamp descending. When <paramref name="action"/>
    /// is non-null, only entries with a matching <c>Action</c> are returned.
    /// </summary>
    List<AuditLogDto> GetRecentEntries(int count, int offset, string? action = null);

    /// <summary>
    /// Returns distinct action names that exist in the log, sorted alphabetically. Used to populate
    /// the filter dropdown on the audit log UI without having to fetch all entries first.
    /// </summary>
    List<string> GetDistinctActions();
}
