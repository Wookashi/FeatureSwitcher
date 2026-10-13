using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;
using Wookashi.FeatureSwitcher.Node.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Node.Database.Tests;

public sealed class FeatureDetailsTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"fs_node_details_{Guid.NewGuid():N}.db");
    private ServiceProvider? _provider;

    #region Repository

    [Theory]
    [InlineData(true)]  // SQLite file database
    [InlineData(false)] // in-memory database
    public void Registration_StoresDetails_AndListReturnsThem(bool useSqlite)
    {
        var (app, feature) = Setup(useSqlite);
        var details = new FeatureDetailsDto(
            "Line one\n\nLine two",
            [new FeatureLinkDto("https://jira.example.com/browse/SHOP-1", null), new FeatureLinkDto("https://wiki.example.com/runbook", "Runbook")]);

        Register(app, new FeatureDto(feature, true) { Details = details });

        var listed = List(app).Single();
        Assert.Equal("Line one\n\nLine two", listed.Description);
        Assert.Collection(
            listed.Links,
            l => { Assert.Equal("https://jira.example.com/browse/SHOP-1", l.Url); Assert.Null(l.Title); },
            l => { Assert.Equal("https://wiki.example.com/runbook", l.Url); Assert.Equal("Runbook", l.Title); });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Registration_ReplacesDetails_AndClearsThemWhenMissing(bool useSqlite)
    {
        var (app, feature) = Setup(useSqlite);
        Register(app, new FeatureDto(feature, true)
        {
            Details = new FeatureDetailsDto("Old", [new FeatureLinkDto("https://example.com/old", null)]),
        });

        Register(app, new FeatureDto(feature, true) { Details = new FeatureDetailsDto("New", []) });
        var replaced = List(app).Single();
        Assert.Equal("New", replaced.Description);
        Assert.Empty(replaced.Links);

        // An older client sends no details at all: the registration is still the source of truth.
        Register(app, new FeatureDto(feature, true));
        var cleared = List(app).Single();
        Assert.Null(cleared.Description);
        Assert.Empty(cleared.Links);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SharedFlag_KeepsDetailsPerApplication(bool useSqlite)
    {
        var (app, feature) = Setup(useSqlite);
        var otherApp = new ApplicationDto(app.Name + "-other");

        Register(app, new FeatureDto(feature, true) { Details = new FeatureDetailsDto("Used by the first app", []) });
        Register(otherApp, new FeatureDto(feature, true) { Details = new FeatureDetailsDto("Used by the second app", []) });

        Assert.Equal("Used by the first app", List(app).Single().Description);
        Assert.Equal("Used by the second app", List(otherApp).Single().Description);
    }

    #endregion

    #region Sanitizer

    [Fact]
    public void Sanitize_ValidDetails_AreUnchanged()
    {
        var problems = new List<string>();
        var details = new FeatureDetailsDto("Text\n\n- item", [new FeatureLinkDto("https://example.com/a", "A")]);

        var result = FeatureDetailsSanitizer.Sanitize(details, problems);

        Assert.Empty(problems);
        Assert.Equal("Text\n\n- item", result!.Description);
        Assert.Equal("https://example.com/a", result.Links.Single().Url);
    }

    [Fact]
    public void Sanitize_NoDetails_ReturnsNull()
    {
        var problems = new List<string>();

        Assert.Null(FeatureDetailsSanitizer.Sanitize(null, problems));
        Assert.Null(FeatureDetailsSanitizer.Sanitize(new FeatureDetailsDto("  \r\n ", []), problems));
        Assert.Empty(problems);
    }

    [Fact]
    public void Sanitize_NormalizesDescription()
    {
        var result = FeatureDetailsSanitizer.Sanitize(new FeatureDetailsDto("\r\nA  \r\n\r\n\r\nB\r\n", []), []);

        Assert.Equal("A\n\nB", result!.Description);
    }

    [Fact]
    public void Sanitize_TruncatesLongDescription()
    {
        var problems = new List<string>();

        var result = FeatureDetailsSanitizer.Sanitize(
            new FeatureDetailsDto(new string('d', FeatureDetailsSanitizer.MaxDescriptionLength + 50), []), problems);

        Assert.Equal(FeatureDetailsSanitizer.MaxDescriptionLength, result!.Description!.Length);
        Assert.EndsWith("…", result.Description);
        Assert.Contains(problems, p => p.Contains("truncated"));
    }

    [Fact]
    public void Sanitize_RemovesControlCharacters_KeepsTabs()
    {
        var problems = new List<string>();

        var result = FeatureDetailsSanitizer.Sanitize(new FeatureDetailsDto("a\tb\u0007c", []), problems);

        Assert.Equal("a\tbc", result!.Description);
        Assert.Single(problems);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("/relative")]
    [InlineData("not a url")]
    [InlineData("https://user:secret@example.com/")]
    public void Sanitize_DropsInvalidLinks(string url)
    {
        var problems = new List<string>();

        var result = FeatureDetailsSanitizer.Sanitize(
            new FeatureDetailsDto("Text", [new FeatureLinkDto(url, null), new FeatureLinkDto("https://example.com/ok", null)]),
            problems);

        Assert.Equal("https://example.com/ok", result!.Links.Single().Url);
        Assert.Single(problems);
        Assert.DoesNotContain(problems, p => p.Contains("secret"));
    }

    [Fact]
    public void Sanitize_DropsLinkWithoutUrl()
    {
        var problems = new List<string>();

        var result = FeatureDetailsSanitizer.Sanitize(new FeatureDetailsDto(null, [new FeatureLinkDto(null!, "No URL")]), problems);

        Assert.Null(result);
        Assert.Single(problems);
    }

    [Fact]
    public void Sanitize_DropsDuplicatesAndLinksOverTheLimit()
    {
        var problems = new List<string>();
        var links = Enumerable.Range(1, FeatureDetailsSanitizer.MaxLinks + 2)
            .Select(i => new FeatureLinkDto($"https://example.com/{i}", null))
            .Prepend(new FeatureLinkDto("https://example.com/1", "Duplicate"))
            .ToList();

        var result = FeatureDetailsSanitizer.Sanitize(new FeatureDetailsDto(null, links), problems);

        Assert.Equal(FeatureDetailsSanitizer.MaxLinks, result!.Links.Count);
        Assert.Equal(FeatureDetailsSanitizer.MaxLinks, result.Links.Select(l => l.Url).Distinct().Count());
        Assert.Contains(problems, p => p.Contains("duplicate"));
        Assert.Contains(problems, p => p.Contains($"more than {FeatureDetailsSanitizer.MaxLinks} links"));
    }

    [Fact]
    public void Sanitize_FixesLinkTitles()
    {
        var problems = new List<string>();
        var longTitle = new string('t', FeatureDetailsSanitizer.MaxLinkTitleLength + 10);

        var result = FeatureDetailsSanitizer.Sanitize(
            new FeatureDetailsDto(null,
            [
                new FeatureLinkDto("https://example.com/a", "Two\nlines"),
                new FeatureLinkDto("https://example.com/b", longTitle),
                new FeatureLinkDto("https://example.com/c", "   "),
            ]),
            problems);

        Assert.Equal("Two lines", result!.Links[0].Title);
        Assert.Equal(FeatureDetailsSanitizer.MaxLinkTitleLength, result.Links[1].Title!.Length);
        Assert.Null(result.Links[2].Title);
        Assert.Single(problems);
    }

    [Fact]
    public void DeserializeLinks_UnreadableJson_ReturnsNoLinks()
    {
        Assert.Empty(FeatureDetailsSanitizer.DeserializeLinks("{not json"));
        Assert.Empty(FeatureDetailsSanitizer.DeserializeLinks(null));
    }

    #endregion

    private (ApplicationDto App, string Feature) Setup(bool useSqlite)
    {
        var services = new ServiceCollection();
        services.AddDatabase(useSqlite ? $"Data Source={_dbPath}" : string.Empty);
        _provider = services.BuildServiceProvider();

        if (useSqlite)
        {
            using var scope = _provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<FeaturesDataContext>().Database.Migrate();
        }

        // Unique names: the in-memory database is shared by all tests in the process.
        var id = Guid.NewGuid().ToString("N");
        return (new ApplicationDto($"DetailsApp-{id}"), $"DetailsFeature-{id}");
    }

    private void Register(ApplicationDto app, FeatureDto feature)
    {
        using var scope = _provider!.CreateScope();
        scope.ServiceProvider.GetRequiredService<IFeatureRepository>().RegisterApplication(app, [feature]);
    }

    private List<FeatureWithUsageDto> List(ApplicationDto app)
    {
        using var scope = _provider!.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IFeatureRepository>().GetFeaturesWithUsageForApplication(app);
    }

    public void Dispose()
    {
        _provider?.Dispose();
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
