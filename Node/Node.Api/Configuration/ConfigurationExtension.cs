namespace Wookashi.FeatureSwitcher.Node.Api.Configuration;

public static class ConfigurationExtension
{
    public static WebApplicationBuilder ReadConfiguration(this WebApplicationBuilder appBuilder)
    {
        appBuilder.Services.Configure<ManagerSettings>(appBuilder.Configuration.GetSection("ManagerConfiguration"));
        return appBuilder;
    }
}