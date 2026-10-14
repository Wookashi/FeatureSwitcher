using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

/// <summary>
/// Per-day count of flag-state reads by one application (one application-feature link). Registrations
/// are not counted. Rows older than the configured retention are deleted by the daily sweep, and all
/// rows of a link are removed with it (cascade).
/// </summary>
[Index(nameof(ApplicationFeatureId), nameof(UsageDay), IsUnique = true)]
[Index(nameof(UsageDay))]
public sealed class ApplicationFeatureUsageEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ApplicationFeatureId { get; set; }

    [ForeignKey(nameof(ApplicationFeatureId))]
    public ApplicationFeatureEntity ApplicationFeature { get; set; } = null!;

    /// <summary>
    /// UTC date (midnight) bucket for the count.
    /// </summary>
    [Required]
    public DateTime UsageDay { get; set; }

    [Required]
    public long UseCount { get; set; }
}
