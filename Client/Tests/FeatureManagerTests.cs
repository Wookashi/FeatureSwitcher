using System.Net;
using Client.Implementation.Tests.Models;
using Moq;
using Moq.Protected;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation;

namespace Client.Implementation.Tests;

public class FeatureManagerTests
{
    private static readonly string NodeAddress = "http://localhost:5000";
    private readonly FeatureSwitcherBasicClientConfiguration _basicConfig = new(
        applicationName: "TestApp",
        environmentName: "TestEnv",
        environmentNodeAddress: new Uri(NodeAddress));

    private void PrepareRegisterFeaturesOnNodeSetup(Mock<HttpMessageHandler> handlerMock)
    {
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == $"{NodeAddress}/applications"
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created
            });
    }
    
    [Fact]
    public async Task FeatureManager_ThrowErrorWhenFeatureNotExists()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        PrepareRegisterFeaturesOnNodeSetup(handlerMock);
        
        
        var httpClient = new HttpClient(handlerMock.Object);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
        
        
        List<IFeatureStateModel> features = new List<IFeatureStateModel>();
        var manager = await new FeatureManagerBuilder(_basicConfig)
            .AddFeatures(features: features).AddHttpClientFactory(clientFactory: factoryMock.Object).BuildAsync();
        
        await Assert.ThrowsAsync<FeatureNotRegisteredException>(
            () => manager.IsFeatureEnabledAsync("fakeTestFlag"));
    }
    
    [Fact]
    public async Task FeatureManager_CheckFalseFeatureState_NodeUnreachable()
    {
        //Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        PrepareRegisterFeaturesOnNodeSetup(handlerMock);

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
        
        List<IFeatureStateModel> features =
        [
            new FeatureStateTestModel(name: "TestFlag", initialState: false, currentLocalState: false)
        ];
        
        //Act
        var manager = await new FeatureManagerBuilder(_basicConfig)
            .AddFeatures(features: features)
            .AddHttpClientFactory(clientFactory: factoryMock.Object)
            .BuildAsync();
    
        var result = await manager.IsFeatureEnabledAsync("TestFlag");
        
        //Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task FeatureManager_CheckTrueFeatureState_NodeUnreachable()
    {
        //Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        PrepareRegisterFeaturesOnNodeSetup(handlerMock);

        var httpClient = new HttpClient(handlerMock.Object);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
        List<IFeatureStateModel> features =
        [
            new FeatureStateTestModel(name: "TestFlag", initialState: true, currentLocalState: true)
        ];
        
        //Act
        var manager = await new FeatureManagerBuilder(_basicConfig)
            .AddFeatures(features: features)
            .AddHttpClientFactory(clientFactory: factoryMock.Object)
            .BuildAsync();
    
        var result = await manager.IsFeatureEnabledAsync("TestFlag");
        
        //Assert
        Assert.True(result);
    }
}