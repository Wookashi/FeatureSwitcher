namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class UserDto(int id, string username, string role, DateTime createdAt, DateTime updatedAt, List<int> accessibleNodeIds)
{
    public int Id { get; set; } = id;
    public string Username { get; set; } = username;
    public string Role { get; set; } = role;
    public DateTime CreatedAt { get; set; } = createdAt;
    public DateTime UpdatedAt { get; set; } = updatedAt;
    public List<int> AccessibleNodeIds { get; set; } = accessibleNodeIds;
}
