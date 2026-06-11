using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Moq.Protected;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;
using Wookashi.FeatureSwitcher.Client.Implementation;

namespace Wookashi.FeatureSwitcher.Client.Implementation.Tests;

public class ConfigureServicesExtensionsTests
{
    private const string NodeAddress = "http://localhost:5000/";
    private const string AppName = "TestApp";
    private const string EnvironmentName = "TestEnv";

    private static FeatureSwitcherBasicClientConfiguration CreateConfig(bool allowStartWithoutNode = false)
        => new(AppName, EnvironmentName, new Uri(NodeAddress), allowStartWithoutNode);

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

    private static void RegisterMockHttpClientFactory(ServiceCollection services, Mock<HttpMessageHandler> handlerMock)
    {
        services.AddSingleton<IHttpClientFactory>(_ =>
        {
            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return factoryMock.Object;
        });
    }

    [Fact]
    public void AddFeatureFlags_RegistersIFeatureManager()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var services = new ServiceCollection();
        RegisterMockHttpClientFactory(services, handlerMock);

        services.AddFeatureFlags(
            CreateConfig(),
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
    public void AddFeatureFlags_ThrowsArgumentNullException_WhenConfigurationIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddFeatureFlags(
            null!,
            new List<IFeatureStateModel>()));
    }

    [Fact]
    public void AddFeatureFlags_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddFeatureFlags(CreateConfig(), new List<IFeatureStateModel>());

        Assert.Same(services, result);
    }

    [Fact]
    public void AddFeatureFlags_RegistersHttpClient()
    {
        var services = new ServiceCollection();

        services.AddFeatureFlags(CreateConfig(), new List<IFeatureStateModel>());

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
        RegisterMockHttpClientFactory(services, handlerMock);

        services.AddFeatureFlags(
            CreateConfig(),
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
        RegisterMockHttpClientFactory(services, handlerMock);

        services.AddFeatureFlags(CreateConfig(), new List<IFeatureStateModel>());

        var provider = services.BuildServiceProvider();
        var manager = provider.GetService<IFeatureManager>();

        Assert.NotNull(manager);
    }

    [Fact]
    public void AddFeatureFlags_RegistersHostedService()
    {
        var services = new ServiceCollection();

        services.AddFeatureFlags(CreateConfig(), new List<IFeatureStateModel>());

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();

        Assert.Contains(hostedServices, s => s is FeatureSwitcherStartupService);
    }

    [Fact]
    public async Task AddFeatureFlags_PropagatesAllowStartWithoutNode()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException(
                "Connection refused",
                new System.Net.Sockets.SocketException()));

        var services = new ServiceCollection();
        RegisterMockHttpClientFactory(services, handlerMock);

        services.AddFeatureFlags(
            CreateConfig(allowStartWithoutNode: true),
            new List<IFeatureStateModel>());

        var provider = services.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>()
            .Single(s => s is FeatureSwitcherStartupService);

        await hostedService.StartAsync(CancellationToken.None);
    }

    [Fact]
    public void AddFeatureFlags_AcceptsMultipleFeatures()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var services = new ServiceCollection();
        RegisterMockHttpClientFactory(services, handlerMock);

        services.AddFeatureFlags(
            CreateConfig(),
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
