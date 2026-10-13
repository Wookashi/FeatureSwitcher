using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation.Tests;

public class FeatureDetailsTests
{
    private const string NodeAddress = "http://localhost:5000/";

    #region FeatureLink

    [Fact]
    public void FeatureLink_FromString_HasUrlAndNoTitle()
    {
        var link = new FeatureLink("https://jira.example.com/browse/SHOP-1423");

        Assert.Equal("https://jira.example.com/browse/SHOP-1423", link.Url.AbsoluteUri);
        Assert.Null(link.Title);
    }

    [Fact]
    public void FeatureLink_WithTitle_TrimsTitle()
    {
        var link = new FeatureLink("  Rollout plan ", "https://wiki.example.com/rollout");

        Assert.Equal("Rollout plan", link.Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FeatureLink_EmptyTitle_MeansNoTitle(string? title)
    {
        Assert.Null(new FeatureLink(title, "https://wiki.example.com/rollout").Title);
    }

    [Fact]
    public void FeatureLink_ConvertsImplicitlyFromStringAndUri()
    {
        IReadOnlyList<FeatureLink> links =
        [
            "https://jira.example.com/browse/SHOP-1423",
            new Uri("https://grafana.example.com/d/checkout"),
            new("Rollout plan", "https://wiki.example.com/rollout"),
        ];

        Assert.Equal(
            ["https://jira.example.com/browse/SHOP-1423", "https://grafana.example.com/d/checkout", "https://wiki.example.com/rollout"],
            links.Select(l => l.Url.AbsoluteUri));
    }

    [Theory]
    [InlineData("jira.example.com/browse/SHOP-1")]
    [InlineData("/relative/path")]
    [InlineData("not a url")]
    [InlineData("htps://jira.example.com")]
    [InlineData("ftp://files.example.com/spec.pdf")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:team@example.com")]
    public void FeatureLink_RejectsNonHttpUrls(string url)
    {
        var ex = Assert.Throws<ArgumentException>(() => new FeatureLink(url));

        Assert.Contains("must be an absolute http or https URL", ex.Message);
    }

    [Fact]
    public void FeatureLink_RejectsRelativeUri()
    {
        Assert.Throws<ArgumentException>(() => new FeatureLink(new Uri("/docs", UriKind.Relative)));
    }

    [Fact]
    public void FeatureLink_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FeatureLink((string)null!));
        Assert.Throws<ArgumentNullException>(() => new FeatureLink((Uri)null!));
    }

    [Fact]
    public void FeatureLink_RejectsCredentials_WithoutEchoingThem()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FeatureLink("https://user:s3cret@wiki.example.com/page"));

        Assert.Contains("credentials", ex.Message);
        Assert.DoesNotContain("s3cret", ex.Message);
    }

    [Fact]
    public void FeatureLink_RejectsTooLongUrl()
    {
        var url = "https://example.com/" + new string('a', FeatureDetailsLimits.MaxUrlLength);

        var ex = Assert.Throws<ArgumentException>(() => new FeatureLink(url));

        Assert.Contains($"the limit is {FeatureDetailsLimits.MaxUrlLength}", ex.Message);
    }

    [Fact]
    public void FeatureLink_RejectsTooLongTitle()
    {
        var title = new string('t', FeatureDetailsLimits.MaxLinkTitleLength + 1);

        var ex = Assert.Throws<ArgumentException>(() => new FeatureLink(title, "https://example.com"));

        Assert.Contains($"the limit is {FeatureDetailsLimits.MaxLinkTitleLength}", ex.Message);
    }

    [Fact]
    public void FeatureLink_RejectsMultiLineTitle()
    {
        Assert.Throws<ArgumentException>(() => new FeatureLink("Rollout\nplan", "https://example.com"));
    }

    [Fact]
    public void FeatureLink_SwappedArguments_ExplainsTheOrder()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FeatureLink("https://wiki.example.com/rollout", "Rollout plan"));

        Assert.Contains("the title goes first", ex.Message);
    }

    #endregion

    #region FeatureStateModel

    [Fact]
    public void FeatureStateModel_HasNoDetailsByDefault()
    {
        var model = new FeatureStateModel("NewCheckout");

        Assert.Null(model.Description);
        Assert.Empty(model.Links);
    }

    [Fact]
    public void Description_RawStringLiteral_IsKeptAsWritten()
    {
        var model = new FeatureStateModel("NewCheckout")
        {
            Description = """
                Replaces the legacy checkout.

                When OFF:
                - customers are redirected to /checkout/legacy
                """,
        };

        Assert.Equal(
            "Replaces the legacy checkout.\n\nWhen OFF:\n- customers are redirected to /checkout/legacy",
            model.Description);
    }

    [Fact]
    public void Description_IsNormalized()
    {
        var model = new FeatureStateModel("NewCheckout")
        {
            Description = "\r\n\r\n  First line   \r\nSecond line\t\r\n\r\n\r\n\r\nThird paragraph\n\n\n",
        };

        Assert.Equal("  First line\nSecond line\n\nThird paragraph", model.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\n  \t\n")]
    public void Description_Blank_MeansNoDescription(string description)
    {
        Assert.Null(new FeatureStateModel("NewCheckout") { Description = description }.Description);
    }

    [Fact]
    public void Description_AtLimitAfterNormalization_IsAccepted()
    {
        var description = new string('d', FeatureDetailsLimits.MaxDescriptionLength) + "   \r\n\r\n";

        var model = new FeatureStateModel("NewCheckout") { Description = description };

        Assert.Equal(FeatureDetailsLimits.MaxDescriptionLength, model.Description!.Length);
    }

    [Fact]
    public void Description_TooLong_ThrowsWithFeatureNameAndLimit()
    {
        var description = new string('d', FeatureDetailsLimits.MaxDescriptionLength + 1);

        var ex = Assert.Throws<ArgumentException>(() => new FeatureStateModel("NewCheckout") { Description = description });

        Assert.Contains("Feature 'NewCheckout'", ex.Message);
        Assert.Contains($"the limit is {FeatureDetailsLimits.MaxDescriptionLength}", ex.Message);
    }

    [Fact]
    public void Description_ControlCharacter_Throws_ButTabIsAllowed()
    {
        Assert.Equal("a\tb", new FeatureStateModel("F") { Description = "a\tb" }.Description);

        var ex = Assert.Throws<ArgumentException>(() => new FeatureStateModel("F") { Description = "bell\u0007" });
        Assert.Contains("U+0007", ex.Message);
    }

    [Fact]
    public void Links_UpToLimit_AreAccepted()
    {
        var model = new FeatureStateModel("NewCheckout")
        {
            Links = Enumerable.Range(1, FeatureDetailsLimits.MaxLinks)
                .Select(i => new FeatureLink($"https://example.com/{i}"))
                .ToList(),
        };

        Assert.Equal(FeatureDetailsLimits.MaxLinks, model.Links.Count);
    }

    [Fact]
    public void Links_OverLimit_ThrowWithFeatureName()
    {
        var links = Enumerable.Range(1, FeatureDetailsLimits.MaxLinks + 1)
            .Select(i => new FeatureLink($"https://example.com/{i}"))
            .ToList();

        var ex = Assert.Throws<ArgumentException>(() => new FeatureStateModel("NewCheckout") { Links = links });

        Assert.Contains("Feature 'NewCheckout'", ex.Message);
        Assert.Contains($"the limit is {FeatureDetailsLimits.MaxLinks}", ex.Message);
    }

    [Fact]
    public void Links_Duplicate_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FeatureStateModel("NewCheckout")
        {
            Links = ["https://example.com/a", new("Same page", "https://example.com/a")],
        });

        Assert.Contains("more than once", ex.Message);
    }

    [Fact]
    public void Links_NullEntry_Throws()
    {
        Assert.Throws<ArgumentException>(() => new FeatureStateModel("NewCheckout") { Links = [null!] });
    }

    [Fact]
    public void Links_Null_ClearsLinks()
    {
        var model = new FeatureStateModel("NewCheckout") { Links = ["https://example.com/a"] };

        model.Links = null!;

        Assert.Empty(model.Links);
    }

    [Fact]
    public void Links_AreCopied()
    {
        var source = new List<FeatureLink> { "https://example.com/a" };
        var model = new FeatureStateModel("NewCheckout") { Links = source };

        source.Add("https://example.com/b");

        Assert.Single(model.Links);
    }

    #endregion

    #region FeatureManager

    [Fact]
    public void FeatureManager_RejectsInvalidCustomDetails()
    {
        var feature = new CustomFeature("Custom", new string('d', FeatureDetailsLimits.MaxDescriptionLength + 1), []);

        var ex = Assert.Throws<ArgumentException>(() => CreateFeatureManager([feature], new Mock<HttpMessageHandler>()));

        Assert.Contains("Feature 'Custom'", ex.Message);
    }

    [Fact]
    public async Task Registration_SendsNormalizedDetails_AndOmitsMissingOnes()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        string? body = null;
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken _) =>
            {
                body = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Created);
            });

        var manager = CreateFeatureManager(
        [
            new FeatureStateModel("Plain", initialState: true),
            new FeatureStateModel("Described")
            {
                Description = "Line one\r\n\r\n\r\nLine two  ",
                Links = ["https://jira.example.com/browse/SHOP-1", new("Runbook", "https://wiki.example.com/runbook")],
            },
            new CustomFeature("Custom", "  Custom description  ", []),
        ], handlerMock);

        await manager.RegisterFeaturesOnNodeAsync();

        using var json = JsonDocument.Parse(body!);
        var features = json.RootElement.GetProperty("Features").EnumerateArray().ToList();

        Assert.False(features[0].TryGetProperty("Description", out _));
        Assert.False(features[0].TryGetProperty("Links", out _));

        Assert.Equal("Line one\n\nLine two", features[1].GetProperty("Description").GetString());
        var links = features[1].GetProperty("Links").EnumerateArray().ToList();
        Assert.Equal("https://jira.example.com/browse/SHOP-1", links[0].GetProperty("Url").GetString());
        Assert.False(links[0].TryGetProperty("Title", out _));
        Assert.Equal("Runbook", links[1].GetProperty("Title").GetString());

        Assert.Equal("  Custom description", features[2].GetProperty("Description").GetString());
        Assert.False(features[2].TryGetProperty("Links", out _));
    }

    private static FeatureManager CreateFeatureManager(List<IFeatureStateModel> features, Mock<HttpMessageHandler> handlerMock)
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handlerMock.Object));
        return new FeatureManager("TestApp", "TestEnv", new Uri(NodeAddress), features, factoryMock.Object);
    }

    // A user-defined feature type that bypasses FeatureStateModel's setters.
    private sealed class CustomFeature(string name, string? description, IReadOnlyList<FeatureLink> links)
        : IFeatureStateModel, IFeatureDetails
    {
        public string Name { get; } = name;
        public bool InitialState => false;
        public bool CurrentLocalState { get; set; }
        public string? Description { get; } = description;
        public IReadOnlyList<FeatureLink> Links { get; } = links;
    }

    #endregion
}
