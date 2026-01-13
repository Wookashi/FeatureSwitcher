# Create migrations for Node.Api
### In FeatureSwitcher catalog
```
dotnet ef migrations add InitialCreate --project Node/Database --startup-project Node/Api --context FeaturesDataContext
```