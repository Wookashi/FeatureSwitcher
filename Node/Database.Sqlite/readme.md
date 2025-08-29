# FeatureSwitcher's SQLite Database Provider

This project contains FeatureSwitcher's SQLite database provider.

## Migrations

Add a migration with:

```
dotnet ef migrations add MigrationName --context SqliteContext --output-dir Migrations --startup-project ..\Node\Node.Api.csproj

dotnet ef database update --context SqliteContext
```
