namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class UserDto(int id, string username, bool isSystemAdmin, DateTime createdAt, DateTime updatedAt, List<NodeAccessDto> nodeAccess)
{
    public int Id { get; set; } = id;
    public string Username { get; set; } = username;
    public bool IsSystemAdmin { get; set; } = isSystemAdmin;
    public DateTime CreatedAt { get; set; } = createdAt;
    public DateTime UpdatedAt { get; set; } = updatedAt;
    public List<NodeAccessDto> NodeAccess { get; set; } = nodeAccess;
}
