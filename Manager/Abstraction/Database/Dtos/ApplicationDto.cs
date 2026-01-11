namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class ApplicationDto(int id, string name)
{
    public int Id { get; set; } = id;    
    public string Name { get; set; } = name;
}