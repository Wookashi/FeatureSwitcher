using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

[Index(nameof(Name), IsUnique = true)]
public class ApplicationEntity
{
    [Key] 
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Environment { get; set; } = null!;
    
    public IEnumerable<FeatureEntity> Features { get; set; }  = new List<FeatureEntity>();
}