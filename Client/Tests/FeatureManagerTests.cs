using System.Net;
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

    private static async Task<FeatureManager> CreateFeatureManagerAsync(
        List<IFeatureStateModel> features,
        Mock<IHttpClientFactory> factoryMock)
    {
        var manager = new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            features,
            factoryMock.Object);

        await manager.RegisterFeaturesOnNodeAsync();
        return manager;
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_ThrowsWhenFeatureNotRegistered()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>();
        var manager = await CreateFeatureManagerAsync(features, factoryMock);

        // Act & Assert
        await Assert.ThrowsAsync<FeatureNotRegisteredException>(
            () => manager.IsFeatureEnabledAsync("nonExistentFeature"));
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_ReturnsFalse_WhenNodeUnreachableAndInitialStateIsFalse()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("TestFlag", initialState: false),
        };
        var manager = await CreateFeatureManagerAsync(features, factoryMock);

        // Act
        var result = await manager.IsFeatureEnabledAsync("TestFlag");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_ReturnsTrue_WhenNodeUnreachableAndInitialStateIsTrue()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        SetupRegistrationEndpoint(handlerMock);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("TestFlag", initialState: true),
        };
        var manager = await CreateFeatureManagerAsync(features, factoryMock);

        // Act
        var result = await manager.IsFeatureEnabledAsync("TestFlag");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Constructor_ThrowsWhenDuplicateFeatureNames()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var factoryMock = CreateMockHttpClientFactory(handlerMock);

        var features = new List<IFeatureStateModel>
        {
            new FeatureStateModel("DuplicateName", initialState: false),
            new FeatureStateModel("DuplicateName", initialState: true),
        };

        // Act & Assert
        Assert.Throws<FeatureNameCollisionException>(() => new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            features,
            factoryMock.Object));
    }
}
