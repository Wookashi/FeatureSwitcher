using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

[Index(nameof(Name), IsUnique = true)]
public class ApplicationEntity
{
    [Key] private int Id { get; set; }

    [Required]
    [MaxLength(100)]
public string Name { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Environment { get; set; } = null!;
    
    public IEnumerable<FeatureEntity> Features { get; set; }
}