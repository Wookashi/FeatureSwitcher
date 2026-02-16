using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database.Repositories;

internal class AuditLogRepository : IAuditLogRepository
{
    private readonly INodeDataContext _context;

    public AuditLogRepository(INodeDataContext context)
    {
        _context = context;
    }

    public void AddEntry(string username, string action, string? details)
    {
        _context.AuditLogs.Add(new AuditLogEntity
        {
            Username = username,
            Action = action,
            Details = details,
            Timestamp = DateTime.UtcNow,
        });
        _context.SaveChanges();
    }

    public List<AuditLogDto> GetRecentEntries(int count, int offset)
    {
        return _context.AuditLogs
            .OrderByDescending(e => e.Timestamp)
            .Skip(offset)
            .Take(count)
            .Select(e => new AuditLogDto(e.Id, e.Username, e.Action, e.Details, e.Timestamp))
            .ToList();
    }
}
