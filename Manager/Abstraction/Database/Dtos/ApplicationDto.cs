namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class ApplicationDto(string name)
{
    public string Name { get; set; } = name;
}