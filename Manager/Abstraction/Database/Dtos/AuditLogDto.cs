namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class AuditLogDto(long id, string username, string action, string? details, DateTime timestamp)
{
    public long Id { get; set; } = id;
    public string Username { get; set; } = username;
    public string Action { get; set; } = action;
    public string? Details { get; set; } = details;
    public DateTime Timestamp { get; set; } = timestamp;
}
