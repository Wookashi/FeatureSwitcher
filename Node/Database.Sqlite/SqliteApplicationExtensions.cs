using System;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Core;
using Wookashi.FeatureSwitcher.Node.Core.Configuration;

namespace Node.Database.Sqlite;

public static class SqliteApplicationExtensions
{
    public static NodeApplication AddSqliteDatabase(this NodeApplication app)
    {
        app.Services.AddBaGetDbContextProvider<SqliteContext>("Sqlite");

        return app;
    }

    public static NodeApplication AddSqliteDatabase(
        this NodeApplication app,
        Action<DatabaseOptions> configure)
    {
        app.AddSqliteDatabase();
        app.Services.Configure(configure);
        return app;
    }
}
