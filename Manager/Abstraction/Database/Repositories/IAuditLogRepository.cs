using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

public interface IAuditLogRepository
{
    void AddEntry(string username, string action, string? details);
    List<AuditLogDto> GetRecentEntries(int count, int offset);
}
