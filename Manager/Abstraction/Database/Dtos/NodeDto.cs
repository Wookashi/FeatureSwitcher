namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class NodeDto(int id, string name, string address)
{
    public int Id { get; set; } = id;    
    public string Name { get; set; } = name;
    public string Address { get; set; } = address;
}