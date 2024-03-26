using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

internal sealed class FeatureEntity
{
    [Key]
    private int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
    
    [Required]
    public bool IsEnabled { get; set; }
    
    public ApplicationEntity Application { get; set; }
}