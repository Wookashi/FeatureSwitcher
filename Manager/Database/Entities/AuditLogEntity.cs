using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

// Index used by the list ordered by time and by the retention cleanup.
[Index(nameof(Timestamp))]
public class AuditLogEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(64)]
    public required string Username { get; set; }

    [Required]
    [MaxLength(64)]
    public required string Action { get; set; }

    [MaxLength(512)]
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; }
}
