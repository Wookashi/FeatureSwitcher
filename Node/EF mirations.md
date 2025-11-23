# Create migrations for Manager.Api
### In FeatureSwitcher catalog
```
dotnet ef migrations add InitialCreate --project Manager/Database --startup-project Manager/Api --context FeatureStatesDataContext
```