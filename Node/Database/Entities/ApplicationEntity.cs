using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

[Index(nameof(Name), IsUnique = true)]
public class ApplicationEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public DateTime? PendingDeletionSince { get; set; }

    [Required]
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    public IEnumerable<ApplicationFeatureEntity> ApplicationFeatures { get; set; } = new List<ApplicationFeatureEntity>();
}
