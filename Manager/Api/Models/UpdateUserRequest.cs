namespace Wookashi.FeatureSwitcher.Manager.Api.Models;

public sealed class UpdateUserRequest
{
    public bool? IsSystemAdmin { get; set; }
    public List<NodeAccessRequest>? NodeAccess { get; set; }
}
