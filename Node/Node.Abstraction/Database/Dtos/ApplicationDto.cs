namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

public sealed class ApplicationDto(string appName, string environment)
{
    public string AppName { get; set; } = appName;
    public string Environment { get; set; } = environment;
    
}