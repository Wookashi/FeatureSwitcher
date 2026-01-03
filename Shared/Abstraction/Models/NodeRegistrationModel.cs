namespace Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

public sealed class NodeRegistrationModel
{
    public required string NodeName { get; set; }
    public required Uri NodeAddress { get; set; }
}