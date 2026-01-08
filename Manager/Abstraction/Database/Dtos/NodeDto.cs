namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class NodeDto(string name, Uri address)
{
    public string Name { get; set; } = name;
    public Uri Address { get; set; } = address;
}