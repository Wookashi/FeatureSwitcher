namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class NodeDto
{
    public required string Name { get; set; }
    public required Uri Address { get; set; }
}