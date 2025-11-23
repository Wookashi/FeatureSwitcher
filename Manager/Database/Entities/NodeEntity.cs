using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

[Index(nameof(Name), IsUnique = true)]
public class NodeEntity
{
    [Key] 
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public required string Name { get; set; }
    
    [MaxLength(256)]
    public string Description { get; set; } = null!;
    
    public ICollection<FeatureStateEntity> FeatureStates { get; set; } = new List<FeatureStateEntity>();
}