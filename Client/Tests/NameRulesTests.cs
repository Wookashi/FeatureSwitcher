using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation.Tests;

public class NameRulesTests
{
    private const string NodeAddress = "http://localhost:5000/";

    [Theory]
    [InlineData("NewCheckout")]
    [InlineData("billing.retry-v2_beta")]
    [InlineData("Feature with spaces")] // discouraged, but works
    [InlineData("Zażółć")]              // discouraged, but works
    [InlineData("Feature:123")]         // discouraged, but works
    public void FeatureName_AllowedByRequiredRules(string name)
    {
        Assert.Equal(name, new FeatureStateModel(name).Name);
    }

    [Fact]
    public void FeatureName_UpToMaxLength_IsAllowed()
    {
        var name = "F" + new string('x', NameLimits.MaxLength - 1);

        Assert.Equal(NameLimits.MaxLength, new FeatureStateModel(name).Name.Length);
    }

    [Fact]
    public void FeatureName_OverMaxLength_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FeatureStateModel(new string('x', NameLimits.MaxLength + 1)));

        Assert.Contains($"the limit is {NameLimits.MaxLength}", ex.Message);
    }

    [Theory]
    [InlineData("A/B")]
    [InlineData("A\\B")]
    [InlineData("A?B")]
    [InlineData("A#B")]
    [InlineData("A%B")]
    [InlineData("A\tB")]
    [InlineData(" Leading")]
    [InlineData("Trailing ")]
    public void FeatureName_BreakingUrls_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => new FeatureStateModel(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("App/Name")]
    [InlineData(" App")]
    public void ApplicationName_BreakingUrls_ThrowsInFeatureManager(string applicationName)
    {
        Assert.Throws<ArgumentException>(() => CreateManager(applicationName, [new FeatureStateModel("Flag")]));
    }

    [Fact]
    public void CustomFeatureType_WithInvalidName_ThrowsInFeatureManager()
    {
        Assert.Throws<ArgumentException>(() => CreateManager("App", [new CustomFeature("A?B")]));
    }

    [Fact]
    public void RecommendedNames_LogNoWarning()
    {
        var logger = new Mock<ILogger<FeatureManager>>();

        CreateManager("App", [new FeatureStateModel("NewCheckout"), new FeatureStateModel("billing.retry-v2")], logger: logger);

        logger.VerifyLog(LogLevel.Warning, Times.Never());
    }

    [Fact]
    public void DiscouragedNames_LogOneWarningListingThem()
    {
        var logger = new Mock<ILogger<FeatureManager>>();
        var longName = "F" + new string('x', NameLimits.RecommendedMaxLength);

        CreateManager(
            "Application With Spaces",
            [new FeatureStateModel(longName), new FeatureStateModel("Zażółć"), new FeatureStateModel("Fine")],
            logger: logger);

        var message = logger.VerifyLog(LogLevel.Warning, Times.Once());
        Assert.Contains("3 name(s)", message);
        Assert.Contains("application 'Application With Spaces' (contains ' ')", message);
        Assert.Contains($"feature '{longName}' ({longName.Length} characters)", message);
        Assert.Contains("feature 'Zażółć' (contains 'ż')", message);
        Assert.DoesNotContain("'Fine'", message);
    }

    [Fact]
    public async Task FeatureStateRequest_EscapesNames()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        Uri? requested = null;
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback((HttpRequestMessage request, CancellationToken _) => requested = request.RequestUri)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("true") });

        var manager = CreateManager("My App", [new FeatureStateModel("Feature:1 ż")], handler);
        await manager.IsFeatureEnabledAsync("Feature:1 ż");

        Assert.Equal(
            "/applications/My%20App/features/Feature%3A1%20%C5%BC/state/",
            requested!.AbsolutePath);
    }

    private static FeatureManager CreateManager(
        string applicationName,
        List<IFeatureStateModel> features,
        Mock<HttpMessageHandler>? handler = null,
        Mock<ILogger<FeatureManager>>? logger = null)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient((handler ?? new Mock<HttpMessageHandler>()).Object));
        return new FeatureManager(applicationName, "TestEnv", new Uri(NodeAddress), features, factory.Object, logger?.Object);
    }

    private sealed class CustomFeature(string name) : IFeatureStateModel
    {
        public string Name { get; } = name;
        public bool InitialState => false;
        public bool CurrentLocalState { get; set; }
    }
}

internal static class LoggerMockExtensions
{
    /// <summary>
    /// Verifies the number of entries at <paramref name="level"/> and returns the last message.
    /// </summary>
    public static string VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel level, Times times)
    {
        logger.Verify(
            l => l.Log(level, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

        var invocation = logger.Invocations.LastOrDefault(i => i.Method.Name == "Log" && i.Arguments[0] is LogLevel l && l == level);
        return invocation?.Arguments[2]?.ToString() ?? string.Empty;
    }
}
