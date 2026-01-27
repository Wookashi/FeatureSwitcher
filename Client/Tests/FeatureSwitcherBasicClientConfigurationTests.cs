using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Implementation;

namespace Client.Implementation.Tests;

public class FeatureSwitcherBasicClientConfigurationTests
{
    [Fact]
    public void Constructor_SetsApplicationNameCorrectly()
    {
        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: "MyApp",
            environmentName: "Production",
            nodeAddress: new Uri("http://localhost:8081/"));

        Assert.Equal("MyApp", config.ApplicationName);
    }

    [Fact]
    public void Constructor_SetsEnvironmentNameCorrectly()
    {
        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: "MyApp",
            environmentName: "Production",
            nodeAddress: new Uri("http://localhost:8081/"));

        Assert.Equal("Production", config.EnvironmentName);
    }

    [Fact]
    public void Constructor_SetsNodeAddressCorrectly()
    {
        var expectedUri = new Uri("http://localhost:8081/");

        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: "MyApp",
            environmentName: "Production",
            nodeAddress: expectedUri);

        Assert.Equal(expectedUri, config.NodeAddress);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenNodeAddressIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FeatureSwitcherBasicClientConfiguration(
            applicationName: "MyApp",
            environmentName: "Production",
            nodeAddress: null));
    }

    [Fact]
    public void Constructor_AcceptsNullApplicationName()
    {
        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: null!,
            environmentName: "Production",
            nodeAddress: new Uri("http://localhost:8081/"));

        Assert.Null(config.ApplicationName);
    }

    [Fact]
    public void Constructor_AcceptsNullEnvironmentName()
    {
        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: "MyApp",
            environmentName: null!,
            nodeAddress: new Uri("http://localhost:8081/"));

        Assert.Null(config.EnvironmentName);
    }

    [Fact]
    public void Constructor_AcceptsEmptyApplicationName()
    {
        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: "",
            environmentName: "Production",
            nodeAddress: new Uri("http://localhost:8081/"));

        Assert.Equal("", config.ApplicationName);
    }

    [Fact]
    public void Constructor_AcceptsEmptyEnvironmentName()
    {
        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: "MyApp",
            environmentName: "",
            nodeAddress: new Uri("http://localhost:8081/"));

        Assert.Equal("", config.EnvironmentName);
    }

    [Fact]
    public void ImplementsIFeatureSwitcherBasicClientConfiguration()
    {
        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: "MyApp",
            environmentName: "Production",
            nodeAddress: new Uri("http://localhost:8081/"));

        Assert.IsAssignableFrom<IFeatureSwitcherBasicClientConfiguration>(config);
    }

    [Fact]
    public void Constructor_AcceptsHttpsUri()
    {
        var expectedUri = new Uri("https://node.example.com:443/");

        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: "MyApp",
            environmentName: "Production",
            nodeAddress: expectedUri);

        Assert.Equal(expectedUri, config.NodeAddress);
    }

    [Fact]
    public void Constructor_AcceptsUriWithPath()
    {
        var expectedUri = new Uri("http://localhost:8081/api/v1/");

        var config = new FeatureSwitcherBasicClientConfiguration(
            applicationName: "MyApp",
            environmentName: "Production",
            nodeAddress: expectedUri);

        Assert.Equal(expectedUri, config.NodeAddress);
    }
}
