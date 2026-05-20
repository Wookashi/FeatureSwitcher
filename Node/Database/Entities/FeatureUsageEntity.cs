using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

/// <summary>
/// Per-day usage counter for a feature. A row is upserted on every state read and on every
/// registration that includes the feature. Rows are removed when the feature is permanently
/// deleted (cascade).
/// </summary>
[Index(nameof(FeatureId), nameof(UsageDay), IsUnique = true)]
public sealed class FeatureUsageEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FeatureId { get; set; }

    [ForeignKey(nameof(FeatureId))]
    public FeatureEntity Feature { get; set; } = null!;

    /// <summary>
    /// UTC date (midnight) bucket for the count.
    /// </summary>
    [Required]
    public DateTime UsageDay { get; set; }

    [Required]
    public long UseCount { get; set; }
}
