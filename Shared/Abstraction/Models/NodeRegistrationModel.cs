namespace Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

public sealed class NodeRegistrationModel
{
    public string NodeName { get; set; } = string.Empty;
    public Uri NodeAddress { get; set; } = new(string.Empty);
}