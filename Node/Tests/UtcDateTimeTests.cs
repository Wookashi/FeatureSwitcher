using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Database.Entities;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Json;

namespace Wookashi.FeatureSwitcher.Node.Database.Tests;

public sealed class UtcDateTimeTests
{
    [Theory]
    [InlineData(true)]  // SQLite file database
    [InlineData(false)] // in-memory database
    public void DateTimesReadFromDatabase_AreUtc(bool useSqlite)
    {
        // Regression: SQLite returned DateTimeKind.Unspecified, which was serialized without "Z" and
        // displayed by browsers shifted by the viewer's UTC offset.
        var dbPath = Path.Combine(Path.GetTempPath(), $"fs_node_utc_{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();
        services.AddDatabase(useSqlite ? $"Data Source={dbPath}" : string.Empty);
        var provider = services.BuildServiceProvider();
        try
        {
            var id = Guid.NewGuid().ToString("N");
            var app = new ApplicationDto($"UtcApp-{id}");
            var feature = $"UtcFeature-{id}";

            using (var scope = provider.CreateScope())
            {
                if (useSqlite)
                {
                    scope.ServiceProvider.GetRequiredService<FeaturesDataContext>().Database.Migrate();
                }

                var repository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
                repository.RegisterApplication(app, [new FeatureDto(feature, true)]);

                var context = scope.ServiceProvider.GetRequiredService<IFeaturesDataContext>();
                var link = context.ApplicationFeatures.Single(l => l.Application.Name == app.Name);
                link.Status = EntityStatus.PendingDeletion;
                link.PendingDeletionSince = DateTime.UtcNow;
                context.SaveChanges();
            }

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IFeaturesDataContext>();
                var link = context.ApplicationFeatures.AsNoTracking().Single(l => l.Application.Name == app.Name);

                Assert.Equal(DateTimeKind.Utc, link.LastUsedAt.Kind);
                Assert.Equal(DateTimeKind.Utc, link.PendingDeletionSince!.Value.Kind);

                var repository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
                var pending = repository.GetPendingFeatures().Single(p => p.ApplicationName == app.Name);
                Assert.Equal(DateTimeKind.Utc, pending.LastUsedAt.Kind);
            }
        }
        finally
        {
            provider.Dispose();
            SqliteConnection.ClearAllPools();
            foreach (var path in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    public void JsonConverter_WritesUtcWithZ(DateTimeKind kind)
    {
        var options = new JsonSerializerOptions { Converters = { new UtcDateTimeJsonConverter() } };
        var value = DateTime.SpecifyKind(new DateTime(2026, 10, 13, 18, 2, 36), kind);

        Assert.Equal("\"2026-10-13T18:02:36Z\"", JsonSerializer.Serialize(value, options));
        Assert.Equal("\"2026-10-13T18:02:36Z\"", JsonSerializer.Serialize<DateTime?>(value, options));
    }

    [Fact]
    public void JsonConverter_ConvertsLocalTimeToUtc()
    {
        var options = new JsonSerializerOptions { Converters = { new UtcDateTimeJsonConverter() } };
        var utc = new DateTime(2026, 10, 13, 18, 2, 36, DateTimeKind.Utc);

        Assert.Equal("\"2026-10-13T18:02:36Z\"", JsonSerializer.Serialize(utc.ToLocalTime(), options));
    }

    [Fact]
    public void JsonConverter_ReadsValueWithoutZoneAsUtc()
    {
        // Older nodes send timestamps without "Z"; the Manager must not shift them.
        var options = new JsonSerializerOptions { Converters = { new UtcDateTimeJsonConverter() } };

        var value = JsonSerializer.Deserialize<DateTime>("\"2026-10-13T18:02:36\"", options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(2026, 10, 13, 18, 2, 36, DateTimeKind.Utc), value);
    }
}
