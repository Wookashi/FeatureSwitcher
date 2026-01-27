using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Implementation;

namespace Client.Implementation.Tests;

public class ConfigureServicesExtensionsTests
{
    private const string NodeAddress = "http://localhost:5000/";
    private const string AppName = "TestApp";
    private const string EnvironmentName = "TestEnv";

    private static void SetupRegistrationEndpoint(Mock<HttpMessageHandler> handlerMock)
    {
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == $"{NodeAddress}applications"
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
            });
    }

    [Fact]
    public void AddFeatureFlags_RegistersIFeatureManager()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var services = new ServiceCollection();

        // Replace HttpClient registration to use our mock
        services.AddSingleton<IHttpClientFactory>(_ =>
        {
            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return factoryMock.Object;
        });

        services.AddFeatureFlags(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>
            {
                new FeatureStateModel("TestFeature"),
            });

        var provider = services.BuildServiceProvider();
        var manager = provider.GetService<IFeatureManager>();

        Assert.NotNull(manager);
        Assert.IsType<FeatureManager>(manager);
    }

    [Fact]
    public void AddFeatureFlags_WithConfiguration_RegistersIFeatureManager()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var services = new ServiceCollection();

        services.AddSingleton<IHttpClientFactory>(_ =>
        {
            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return factoryMock.Object;
        });

        var config = new FeatureSwitcherBasicClientConfiguration(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress));

        services.AddFeatureFlags(
            config,
            new List<IFeatureStateModel>
            {
                new FeatureStateModel("TestFeature"),
            });

        var provider = services.BuildServiceProvider();
        var manager = provider.GetService<IFeatureManager>();

        Assert.NotNull(manager);
    }

    [Fact]
    public void AddFeatureFlags_WithConfiguration_ThrowsArgumentNullException_WhenConfigurationIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddFeatureFlags(
            null!,
            new List<IFeatureStateModel>()));
    }

    [Fact]
    public void AddFeatureFlags_ReturnsServiceCollection_ForChaining()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var services = new ServiceCollection();

        var result = services.AddFeatureFlags(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>());

        Assert.Same(services, result);
    }

    [Fact]
    public void AddFeatureFlags_WithConfiguration_ReturnsServiceCollection_ForChaining()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var services = new ServiceCollection();
        var config = new FeatureSwitcherBasicClientConfiguration(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress));

        var result = services.AddFeatureFlags(config, new List<IFeatureStateModel>());

        Assert.Same(services, result);
    }

    [Fact]
    public void AddFeatureFlags_RegistersHttpClient()
    {
        var services = new ServiceCollection();

        services.AddFeatureFlags(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>());

        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetService<IHttpClientFactory>();

        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public void AddFeatureFlags_RegistersAsSingleton()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var services = new ServiceCollection();

        services.AddSingleton<IHttpClientFactory>(_ =>
        {
            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return factoryMock.Object;
        });

        services.AddFeatureFlags(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>
            {
                new FeatureStateModel("TestFeature"),
            });

        var provider = services.BuildServiceProvider();
        var manager1 = provider.GetService<IFeatureManager>();
        var manager2 = provider.GetService<IFeatureManager>();

        Assert.Same(manager1, manager2);
    }

    [Fact]
    public void AddFeatureFlags_AcceptsEmptyFeatureList()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var services = new ServiceCollection();

        services.AddSingleton<IHttpClientFactory>(_ =>
        {
            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return factoryMock.Object;
        });

        services.AddFeatureFlags(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>());

        var provider = services.BuildServiceProvider();
        var manager = provider.GetService<IFeatureManager>();

        Assert.NotNull(manager);
    }

    [Fact]
    public void AddFeatureFlags_AcceptsMultipleFeatures()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var services = new ServiceCollection();

        services.AddSingleton<IHttpClientFactory>(_ =>
        {
            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return factoryMock.Object;
        });

        services.AddFeatureFlags(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>
            {
                new FeatureStateModel("Feature1", initialState: true),
                new FeatureStateModel("Feature2", initialState: false),
                new FeatureStateModel("Feature3", initialState: true),
            });

        var provider = services.BuildServiceProvider();
        var manager = provider.GetService<IFeatureManager>();

        Assert.NotNull(manager);
    }
}
