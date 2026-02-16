namespace Wookashi.FeatureSwitcher.Manager.Api.Models;

public sealed class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<int> NodeIds { get; set; } = [];
}
