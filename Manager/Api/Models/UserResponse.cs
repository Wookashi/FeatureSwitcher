namespace Wookashi.FeatureSwitcher.Manager.Api.Models;

public sealed class UserResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<int> AccessibleNodeIds { get; set; } = [];
}
