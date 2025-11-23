using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Manager.Database;

internal sealed class FeatureStatesInMemoryDataContext(DbContextOptions<FeatureStatesDataContext> options)
    : FeatureStatesDataContext(options)
{
    protected override void OnConfiguring
        (DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "Test_db");
    }
}