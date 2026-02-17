namespace Wookashi.FeatureSwitcher.Manager.Api.Models;

public sealed class UpdateUserRequest
{
    public string? Role { get; set; }
    public List<int>? NodeIds { get; set; }
}
