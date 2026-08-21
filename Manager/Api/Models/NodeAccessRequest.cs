namespace Wookashi.FeatureSwitcher.Manager.Api.Models;

public sealed class NodeAccessRequest
{
    public int NodeId { get; set; }
    public string Role { get; set; } = string.Empty;
}
