using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

[Index(nameof(ApplicationId), nameof(FeatureId), IsUnique = true)]
public sealed class ApplicationFeatureEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public ApplicationEntity Application { get; set; } = null!;

    [Required]
    public int FeatureId { get; set; }

    [ForeignKey(nameof(FeatureId))]
    public FeatureEntity Feature { get; set; } = null!;
}
