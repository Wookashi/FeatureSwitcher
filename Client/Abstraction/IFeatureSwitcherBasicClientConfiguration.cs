namespace Wookashi.FeatureSwitcher.Client.Abstraction;

public interface IFeatureSwitcherBasicClientConfiguration
{
    string ApplicationName { get; }
    string EnvironmentName { get; }
    Uri EnvironmentNodeAddress { get; }
}