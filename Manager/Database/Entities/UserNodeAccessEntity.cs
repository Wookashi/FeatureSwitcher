using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Enums;

namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

[Index(nameof(UserId), nameof(NodeId), IsUnique = true)]
public class UserNodeAccessEntity
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public UserEntity User { get; set; } = null!;

    public int NodeId { get; set; }
    public NodeEntity Node { get; set; } = null!;

    public NodeRoleEnum RoleEnum { get; set; }
}
