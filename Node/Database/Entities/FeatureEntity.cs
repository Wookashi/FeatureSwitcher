using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

[Index(nameof(ApplicationId), nameof(Name), IsUnique = true)]
public sealed class FeatureEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    public bool IsEnabled { get; set; }

    [Required]
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public DateTime? PendingDeletionSince { get; set; }

    [Required]
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public required ApplicationEntity Application { get; set; }

    public IEnumerable<ApplicationFeatureEntity> ApplicationFeatures { get; set; } = new List<ApplicationFeatureEntity>();
}
