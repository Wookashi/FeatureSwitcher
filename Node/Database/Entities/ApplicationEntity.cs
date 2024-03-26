using System.ComponentModel.DataAnnotations;

namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

internal sealed class ApplicationEntity
{
    [Key]
    private int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
    
    public List<FeatureEntity> Features { get; set; }
}