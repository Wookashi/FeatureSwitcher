using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Wookashi.FeatureSwitcher.Manager.Database.Extensions;

internal static class UtcDateTimeConventions
{
    /// <summary>
    /// All timestamps are written as UTC, but SQLite stores them without a time zone, so they come
    /// back with <see cref="DateTimeKind.Unspecified"/>. Such values are serialized without the "Z"
    /// suffix and browsers then read them as local time, shifting every displayed time by the
    /// viewer's UTC offset. This marks every DateTime read from the database as UTC (and converts a
    /// local value to UTC before writing, should one ever be passed in).
    /// </summary>
    internal static void UseUtcDateTimes(this ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
