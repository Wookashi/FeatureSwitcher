namespace Wookashi.FeatureSwitcher.Manager.Api.Models;

public sealed class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsSystemAdmin { get; set; }
    public List<NodeAccessRequest> NodeAccess { get; set; } = [];
}
