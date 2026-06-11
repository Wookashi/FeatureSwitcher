using System.Net;
using Moq;
using Moq.Protected;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;
using Wookashi.FeatureSwitcher.Client.Implementation;

namespace Wookashi.FeatureSwitcher.Client.Implementation.Tests;

public class FeatureSwitcherStartupServiceTests
{
    private const string NodeAddress = "http://localhost:5000/";
    private const string AppName = "TestApp";
    private const string EnvironmentName = "TestEnv";

    private static FeatureManager CreateManagerWithUnreachableNode()
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

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>
            {
                new FeatureStateModel("TestFlag", initialState: true),
            },
            factoryMock.Object);
    }

    private static FeatureManager CreateManagerWithReachableNode()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Created });

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new FeatureManager(
            AppName,
            EnvironmentName,
            new Uri(NodeAddress),
            new List<IFeatureStateModel>(),
            factoryMock.Object);
    }

    [Fact]
    public async Task StartAsync_ThrowsNodeUnreachableException_WhenAllowStartWithoutNodeIsFalse()
    {
        var manager = CreateManagerWithUnreachableNode();
        var service = new FeatureSwitcherStartupService(manager, allowStartWithoutNode: false);

        await Assert.ThrowsAsync<NodeUnreachableException>(
            () => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenNodeUnreachableAndAllowStartWithoutNodeIsTrue()
    {
        var manager = CreateManagerWithUnreachableNode();
        var service = new FeatureSwitcherStartupService(manager, allowStartWithoutNode: true);

        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_RegistersSuccessfully_WhenNodeIsReachable()
    {
        var manager = CreateManagerWithReachableNode();
        var service = new FeatureSwitcherStartupService(manager, allowStartWithoutNode: true);

        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_DefaultsToNotAllowingStartWithoutNode()
    {
        var manager = CreateManagerWithUnreachableNode();
        var service = new FeatureSwitcherStartupService(manager);

        await Assert.ThrowsAsync<NodeUnreachableException>(
            () => service.StartAsync(CancellationToken.None));
    }
}
