namespace Wookashi.FeatureSwitcher.Manager.Api.Configuration;

public sealed class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "FeatureSwitcher";
    public string Audience { get; set; } = "FeatureSwitcher";
    public int ExpirationMinutes { get; set; } = 60;
}
