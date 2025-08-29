namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

public sealed class ApplicationDto(string name, string environment)
{
    public string Name { get; set; } = name;
    public string Environment { get; set; } = environment;
    
}