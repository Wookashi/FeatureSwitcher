namespace Wookashi.FeatureSwitcher.Node.Core.Configuration;

public class FeatureSwitcherOptions
{
    /// <summary>
    /// The application root URL for usage in reverse proxy scenarios.
    /// </summary>
    public string PathBase { get; set; }

    /// <summary>
    /// If enabled, the database will be updated at app startup by running
    /// Entity Framework migrations. This is not recommended in production.
    /// </summary>
    public bool RunMigrationsAtStartup { get; set; } = true;
    
    public DatabaseOptions Database { get; set; }

}
