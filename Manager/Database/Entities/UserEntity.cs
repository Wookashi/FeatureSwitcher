using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Enums;

namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

[Index(nameof(Username), IsUnique = true)]
public class UserEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public required string Username { get; set; }

    [Required]
    [MaxLength(256)]
    public required string PasswordHash { get; set; }

    public UserRoleEnum RoleEnum { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<UserNodeAccessEntity> NodeAccess { get; set; } = new List<UserNodeAccessEntity>();
}
