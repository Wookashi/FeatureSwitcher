using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

[Index(nameof(Name), IsUnique = true)]
public sealed class FeatureEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = null!;
    
    [MaxLength(256)]
    public string Description { get; set; } = null!;

    public bool IsArchived { get; set; }
    
    public ICollection<FeatureStateEntity> FeatureStates { get; set; } = new List<FeatureStateEntity>();
}