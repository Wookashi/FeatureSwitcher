using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation.Tests;

public class NodeResilienceTests
{
    private const string NodeAddress = "http://localhost:5000/";

    /// <summary>
    /// Test double for the Node: answers each request with the next queued response for its path, or a
    /// default; records the paths requested.
    /// </summary>
    private sealed class FakeNode : HttpMessageHandler
    {
        private readonly object _lock = new();
        public List<string> Requests { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.Created);

        public int Count(string pathPart)
        {
            lock (_lock) return Requests.Count(r => r.Contains(pathPart));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (_lock) Requests.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(Respond(request));
        }
    }

    private static HttpResponseMessage Unreachable() =>
        throw new HttpRequestException("Connection refused", new SocketException());

    private static HttpResponseMessage State(bool value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(value ? "true" : "false") };

    private static FeatureManager CreateManager(FakeNode node, List<IFeatureStateModel> features, ILogger<FeatureManager>? logger = null)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(node, disposeHandler: false));
        return new FeatureManager("App", "TestEnv", new Uri(NodeAddress), features, factory.Object, logger);
    }

    #region 404: feature not registered on the Node

    [Fact]
    public async Task NotRegisteredFeature_FallsBack_WithoutAffectingOtherFeatures()
    {
        var node = new FakeNode
        {
            Respond = r => r.RequestUri!.AbsolutePath.Contains("/NewFlag/")
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : State(true),
        };
        var manager = CreateManager(node, [new FeatureStateModel("NewFlag", initialState: false), new FeatureStateModel("OldFlag")]);

        Assert.False(await manager.IsFeatureEnabledAsync("NewFlag")); // initial state
        Assert.True(await manager.IsFeatureEnabledAsync("OldFlag"));  // still read from the Node
        Assert.Equal(1, node.Count("/OldFlag/"));
        Assert.True(manager.IsNodeReachable);
    }

    [Fact]
    public async Task NotRegisteredFeature_IsLoggedOncePerFeature()
    {
        var node = new FakeNode { Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound) };
        var logger = new Mock<ILogger<FeatureManager>>();
        var manager = CreateManager(node, [new FeatureStateModel("NewFlag")], logger.Object);

        await manager.IsFeatureEnabledAsync("NewFlag");
        await manager.IsFeatureEnabledAsync("NewFlag");
        await manager.IsFeatureEnabledAsync("NewFlag");

        var message = logger.VerifyLog(LogLevel.Warning, Times.Once());
        Assert.Contains("not registered on node", message);
        Assert.Equal(3, node.Count("/NewFlag/")); // each check still asks the Node
    }

    [Fact]
    public async Task UnreachableNode_StillOpensTheCircuit()
    {
        var node = new FakeNode { Respond = _ => Unreachable() };
        var manager = CreateManager(node, [new FeatureStateModel("A", initialState: true), new FeatureStateModel("B")]);

        Assert.True(await manager.IsFeatureEnabledAsync("A"));
        await manager.IsFeatureEnabledAsync("B");

        Assert.Single(node.Requests); // B served from cache while the circuit is open
        Assert.False(manager.IsNodeReachable);
    }

    #endregion

    #region Status

    [Fact]
    public async Task Status_ReflectsRegistrationAndLatestRequest()
    {
        var node = new FakeNode();
        var manager = CreateManager(node, [new FeatureStateModel("Flag")]);

        Assert.False(manager.IsRegistered);
        Assert.False(manager.IsNodeReachable);
        Assert.Null(manager.LastNodeContactAt);

        await manager.RegisterFeaturesOnNodeAsync();
        Assert.True(manager.IsRegistered);
        Assert.True(manager.IsNodeReachable);
        Assert.NotNull(manager.LastNodeContactAt);

        node.Respond = _ => Unreachable();
        await manager.IsFeatureEnabledAsync("Flag");
        Assert.False(manager.IsNodeReachable);
        Assert.True(manager.IsRegistered);
    }

    #endregion

    #region Background registration

    [Fact]
    public async Task StartWithoutNode_RegistersInBackground_WhenTheNodeComesBack()
    {
        var node = new FakeNode();
        var attempts = 0;
        node.Respond = _ => Interlocked.Increment(ref attempts) <= 3 ? Unreachable() : new HttpResponseMessage(HttpStatusCode.Created);
        var manager = CreateManager(node, [new FeatureStateModel("Flag")]);
        var service = new FeatureSwitcherStartupService(manager, allowStartWithoutNode: true, retryDelays: [TimeSpan.FromMilliseconds(10)]);

        await service.StartAsync(CancellationToken.None);
        Assert.False(manager.IsRegistered);

        await WaitUntil(() => manager.IsRegistered);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(4, node.Count("/applications"));
    }

    [Fact]
    public async Task StartWithoutNode_ServerErrorAtStartup_IsRetriedToo()
    {
        var node = new FakeNode();
        var attempts = 0;
        node.Respond = _ => Interlocked.Increment(ref attempts) == 1
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : new HttpResponseMessage(HttpStatusCode.Created);
        var manager = CreateManager(node, [new FeatureStateModel("Flag")]);
        var service = new FeatureSwitcherStartupService(manager, allowStartWithoutNode: true, retryDelays: [TimeSpan.FromMilliseconds(10)]);

        await service.StartAsync(CancellationToken.None);
        await WaitUntil(() => manager.IsRegistered);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task BackgroundRegistration_StopsOnFinalError()
    {
        var node = new FakeNode();
        var attempts = 0;
        node.Respond = _ => Interlocked.Increment(ref attempts) == 1
            ? Unreachable()
            : new HttpResponseMessage((HttpStatusCode)422); // environment mismatch: retrying won't help
        var manager = CreateManager(node, [new FeatureStateModel("Flag")]);
        var service = new FeatureSwitcherStartupService(manager, allowStartWithoutNode: true, retryDelays: [TimeSpan.FromMilliseconds(10)]);

        await service.StartAsync(CancellationToken.None);
        await WaitUntil(() => node.Count("/applications") >= 2);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(2, node.Count("/applications"));
        Assert.False(manager.IsRegistered);
    }

    [Fact]
    public async Task StopAsync_CancelsPendingRetry()
    {
        var node = new FakeNode { Respond = _ => Unreachable() };
        var manager = CreateManager(node, [new FeatureStateModel("Flag")]);
        var service = new FeatureSwitcherStartupService(manager, allowStartWithoutNode: true, retryDelays: [TimeSpan.FromHours(1)]);

        await service.StartAsync(CancellationToken.None);
        var stopwatch = Stopwatch.StartNew();
        await service.StopAsync(CancellationToken.None);

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5));
        Assert.Single(node.Requests);
    }

    #endregion

    #region Health check

    [Theory]
    [InlineData(false, false, HealthStatus.Degraded)] // not registered
    [InlineData(true, false, HealthStatus.Degraded)]  // registered, Node unreachable now
    [InlineData(true, true, HealthStatus.Healthy)]
    public async Task HealthCheck_ReportsFallbackAsFailureStatus(bool registered, bool reachableNow, HealthStatus expected)
    {
        var node = new FakeNode();
        var manager = CreateManager(node, [new FeatureStateModel("Flag")]);
        if (registered)
        {
            await manager.RegisterFeaturesOnNodeAsync();
        }
        if (!reachableNow)
        {
            node.Respond = _ => Unreachable();
            await manager.IsFeatureEnabledAsync("Flag");
        }

        var check = new FeatureSwitcherHealthCheck(manager);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("feature-switcher", check, HealthStatus.Degraded, null),
        };

        var result = await check.CheckHealthAsync(context);

        Assert.Equal(expected, result.Status);
        Assert.Equal(registered, result.Data["registered"]);
    }

    [Fact]
    public async Task AddFeatureSwitcher_RegistersTheCheck_WithConfigurableFailureStatus()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureFlags(
            new FeatureSwitcherBasicClientConfiguration("App", "TestEnv", new Uri("http://127.0.0.1:1/")),
            [new FeatureStateModel("Flag")]);
        services.AddHealthChecks().AddFeatureSwitcher(failureStatus: HealthStatus.Unhealthy);
        using var provider = services.BuildServiceProvider();

        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.Entries["feature-switcher"].Status); // never registered
    }

    #endregion

    private static async Task WaitUntil(Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > TimeSpan.FromSeconds(10))
            {
                throw new TimeoutException("Condition not met within 10 seconds.");
            }
            await Task.Delay(10);
        }
    }
}
