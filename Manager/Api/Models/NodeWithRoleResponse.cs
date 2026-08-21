namespace Wookashi.FeatureSwitcher.Manager.Api.Models;

public sealed class NodeWithRoleResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
