using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation;

namespace Client.Implementation.Tests;

public class FeatureManagerTests
{
    private const string NodeAddress = "http://localhost:5000/";
    private const string AppName = "TestApp";
    private const string EnvironmentName = "TestEnv";

    private static Mock<IHttpClientFactory> CreateMockHttpClientFactory(Mock<HttpMessageHandler> handlerMock)
    {
        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factoryMock;
    }

    private static void SetupRegistrationEndpoint(Mock<HttpMessageHandler> handlerMock, HttpStatusCode statusCode = HttpStatusCode.Created)
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
                StatusCode = statusCode,
                ReasonPhrase = statusCode == HttpStatusCode.Created ? "Created" : "Error",
            });
    }

    private static void SetupFeatureStateEndpoint(
        Mock<HttpMessageHandler> handlerMock,
        string featureName,
        bool featureState,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == $"{NodeAddress}applications/{AppName}/features/{featureName}/state/"
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(featureState)),
            });
    }

    private static FeatureManager CreateFeatureManager(
        List<IFeatureStateModel> features,
        Mock<IHttpClientFactory> factoryMock)
    {
        return new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            features,
            factoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenApplicationNameIsNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        Assert.Throws<ArgumentNullException>(() => new FeatureManager(
            null!,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>(),
            factoryMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenEnvironmentNameIsNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        Assert.Throws<ArgumentNullException>(() => new FeatureManager(
            AppName,
            null!,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>(),
            factoryMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenNodeAddressIsNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        Assert.Throws<ArgumentNullException>(() => new FeatureManager(
            AppName,
            EnvironmentName,
            null!,
            new List<IFeatureStateModel>(),
            factoryMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenFeaturesIsNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        Assert.Throws<ArgumentNullException>(() => new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            null!,
            factoryMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenHttpClientFactoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>(),
            null!));
    }

    [Fact]
    public void Constructor_ThrowsFeatureNameCollisionException_WhenDuplicateFeatureNames()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("DuplicateName", initialState: false),
            new FeatureStateModel("DuplicateName", initialState: true),
        };

        Assert.Throws<FeatureNameCollisionException>(() => new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            features,
            factoryMock.Object));
    }

    [Fact]
    public void Constructor_Succeeds_WithValidParameters()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("Feature1"),
            new FeatureStateModel("Feature2"),
        };

        var manager = new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            features,
            factoryMock.Object);

        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_Succeeds_WithEmptyFeatureList()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var manager = new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>(),
            factoryMock.Object);

        Assert.NotNull(manager);
    }

    #endregion

    #region IsFeatureEnabledAsync Tests

    [Fact]
    public async Task IsFeatureEnabledAsync_ThrowsFeatureNotRegisteredException_WhenFeatureNotRegistered()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var manager = CreateFeatureManager(new List<IFeatureStateModel>(), factoryMock);
        await manager.RegisterFeaturesOnNodeAsync();

        await Assert.ThrowsAsync<FeatureNotRegisteredException>(
            () => manager.IsFeatureEnabledAsync("nonExistentFeature"));
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_ReturnsCachedFalseState_WhenNodeUnreachable()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("TestFlag", initialState: false),
        };
        var manager = CreateFeatureManager(features, factoryMock);
        await manager.RegisterFeaturesOnNodeAsync();

        var result = await manager.IsFeatureEnabledAsync("TestFlag");

        Assert.False(result);
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_ReturnsCachedTrueState_WhenNodeUnreachable()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("TestFlag", initialState: true),
        };
        var manager = CreateFeatureManager(features, factoryMock);
        await manager.RegisterFeaturesOnNodeAsync();

        var result = await manager.IsFeatureEnabledAsync("TestFlag");

        Assert.True(result);
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_ReturnsNodeState_WhenNodeReturnsTrue()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        SetupFeatureStateEndpoint(handlerMock, "TestFlag", featureState: true);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("TestFlag", initialState: false),
        };
        var manager = CreateFeatureManager(features, factoryMock);
        await manager.RegisterFeaturesOnNodeAsync();

        var result = await manager.IsFeatureEnabledAsync("TestFlag");

        Assert.True(result);
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_ReturnsNodeState_WhenNodeReturnsFalse()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        SetupFeatureStateEndpoint(handlerMock, "TestFlag", featureState: false);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("TestFlag", initialState: true),
        };
        var manager = CreateFeatureManager(features, factoryMock);
        await manager.RegisterFeaturesOnNodeAsync();

        var result = await manager.IsFeatureEnabledAsync("TestFlag");

        Assert.False(result);
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_UpdatesLocalCache_WhenNodeReturnsValue()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        SetupFeatureStateEndpoint(handlerMock, "TestFlag", featureState: true);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var feature = new FeatureStateModel("TestFlag", initialState: false);
        var features = new List<IFeatureStateModel> { feature };
        var manager = CreateFeatureManager(features, factoryMock);
        await manager.RegisterFeaturesOnNodeAsync();

        Assert.False(feature.CurrentLocalState);

        await manager.IsFeatureEnabledAsync("TestFlag");

        Assert.True(feature.CurrentLocalState);
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_FallsBackToCache_WhenNodeReturnsError()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        SetupFeatureStateEndpoint(handlerMock, "TestFlag", featureState: false, HttpStatusCode.InternalServerError);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("TestFlag", initialState: true),
        };
        var manager = CreateFeatureManager(features, factoryMock);
        await manager.RegisterFeaturesOnNodeAsync();

        var result = await manager.IsFeatureEnabledAsync("TestFlag");

        Assert.True(result);
    }

    #endregion

    #region RegisterFeaturesOnNodeAsync Tests

    [Fact]
    public async Task RegisterFeaturesOnNodeAsync_Succeeds_WhenNodeReturnsCreated()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock, HttpStatusCode.Created);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("Feature1"),
        };
        var manager = CreateFeatureManager(features, factoryMock);

        await manager.RegisterFeaturesOnNodeAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RegisterFeaturesOnNodeAsync_ThrowsEnvironmentMismatchException_WhenNodeReturns422()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock, (HttpStatusCode)422);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var manager = CreateFeatureManager(new List<IFeatureStateModel>(), factoryMock);

        await Assert.ThrowsAsync<EnvironmentMismatchException>(
            () => manager.RegisterFeaturesOnNodeAsync());
    }

    [Fact]
    public async Task RegisterFeaturesOnNodeAsync_ThrowsRegistrationException_WhenNodeReturnsOtherError()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock, HttpStatusCode.BadRequest);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var manager = CreateFeatureManager(new List<IFeatureStateModel>(), factoryMock);

        var exception = await Assert.ThrowsAsync<RegistrationException>(
            () => manager.RegisterFeaturesOnNodeAsync());

        Assert.Equal((int)HttpStatusCode.BadRequest, exception.Code);
    }

    [Fact]
    public async Task RegisterFeaturesOnNodeAsync_ThrowsRegistrationException_WhenNodeReturns500()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock, HttpStatusCode.InternalServerError);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var manager = CreateFeatureManager(new List<IFeatureStateModel>(), factoryMock);

        var exception = await Assert.ThrowsAsync<RegistrationException>(
            () => manager.RegisterFeaturesOnNodeAsync());

        Assert.Equal(500, exception.Code);
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task IsFeatureEnabledAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("TestFlag", initialState: false),
        };
        var manager = CreateFeatureManager(features, factoryMock);
        await manager.RegisterFeaturesOnNodeAsync();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => manager.IsFeatureEnabledAsync("TestFlag", cts.Token));
    }

    [Fact]
    public async Task RegisterFeaturesOnNodeAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var manager = CreateFeatureManager(new List<IFeatureStateModel>(), factoryMock);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => manager.RegisterFeaturesOnNodeAsync(cts.Token));
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_WorksWithCancellationToken()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        SetupFeatureStateEndpoint(handlerMock, "TestFlag", featureState: true);

        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("TestFlag", initialState: false),
        };
        var manager = CreateFeatureManager(features, factoryMock);
        await manager.RegisterFeaturesOnNodeAsync();

        using var cts = new CancellationTokenSource();
        var result = await manager.IsFeatureEnabledAsync("TestFlag", cts.Token);

        Assert.True(result);
    }

    [Fact]
    public async Task RegisterFeaturesOnNodeAsync_WorksWithCancellationToken()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);

        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var manager = CreateFeatureManager(new List<IFeatureStateModel>(), factoryMock);

        using var cts = new CancellationTokenSource();
        await manager.RegisterFeaturesOnNodeAsync(cts.Token);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion
}
