using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

[Index(nameof(Name), IsUnique = true)]
public sealed class FeatureEntity(ApplicationEntity application)
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
    
    [Required]
    public bool IsEnabled { get; set; }
    
    public ApplicationEntity Application { get; set; } = application;
}