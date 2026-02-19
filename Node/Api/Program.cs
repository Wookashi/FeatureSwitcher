using Microsoft.OpenApi;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;
using Wookashi.FeatureSwitcher.Node.Api.HealthChecks;
using Wookashi.FeatureSwitcher.Node.Api.Models;
using Wookashi.FeatureSwitcher.Node.Api.Services;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<ManagerSettings>(
    builder.Configuration.GetSection("ManagerSettings"));
builder.Services.Configure<NodeConfiguration>(
    builder.Configuration.GetSection("NodeConfiguration"));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Wookashi Feature Switcher Node API",
        Version = "v1",
    });
});
var dbConnectionString = builder.Configuration["NodeConfiguration:ConnectionString"] ?? string.Empty;
builder.Services.AddDatabase(dbConnectionString);
builder.Services.AddHealthCheckElements();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<ManagerRegistrationHostedService>();

var app = builder.Build();

var nodeConfig = builder.Configuration.GetSection("NodeConfiguration").Get<NodeConfiguration>();
var nodeEnvironment = nodeConfig?.Environment ?? "FailConfigurationSetting";
var managerSettings = builder.Configuration.GetSection("ManagerSettings").Get<ManagerSettings>();

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Node.Startup");
logger.LogInformation("=== Node API Starting ===");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Node Environment: {NodeEnvironment}", nodeEnvironment);
logger.LogInformation("Node Name: {NodeName}", nodeConfig?.Name ?? "(not set)");
logger.LogInformation("Node Address: {NodeAddress}", nodeConfig?.Address ?? "(not set)");
logger.LogInformation("Manager URL: {ManagerUrl}", managerSettings?.Url ?? "(not set)");
logger.LogInformation("Manager Username: {Username}", string.IsNullOrEmpty(managerSettings?.Username) ? "(not set)" : managerSettings.Username);
logger.LogInformation("Database: {ConnectionString}", string.IsNullOrEmpty(dbConnectionString) ? "(in-memory)" : $"{dbConnectionString} (configured)");
logger.LogInformation("============================");

var apiLogger = loggerFactory.CreateLogger("Node.Api");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (dbConnectionString != string.Empty)
{
    app.MigrateDatabase();
}

app.UseHttpsRedirection();

app.UseHealthCheck(
    nodeConfig?.Name ?? "(not set)",
    nodeEnvironment,
    nodeConfig?.Address ?? "(not set)");

app.MapPost("/applications",
        (ApplicationRegistrationRequestModel registerModel, IFeatureRepository featureRepository) =>
        {
            apiLogger.LogInformation("Registering app {AppName} for environment {Environment} with {FeatureCount} feature(s)",
                registerModel.AppName, registerModel.Environment, registerModel.Features?.Count ?? 0);

            if (registerModel.Environment != nodeEnvironment)
            {
                apiLogger.LogWarning("Environment mismatch: node is '{NodeEnv}', request is '{ReqEnv}' for app {AppName}",
                    nodeEnvironment, registerModel.Environment, registerModel.AppName);
                return Results.Problem(
                    title: "Environment mismatch",
                    detail: $"Node environment is '{nodeEnvironment}', but request environment is '{registerModel.Environment}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            var featureService = new FeatureService(featureRepository);
            featureService.RegisterApplication(new ApplicationDto(registerModel.AppName), registerModel.Features ?? []);

            apiLogger.LogInformation("App {AppName} registered successfully", registerModel.AppName);
            return Results.Created();
        })
    .WithDescription("Used to register application by app client. Adds or updates app data in node database.")
    .WithTags("Client")
    .Produces(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status409Conflict);

app.MapGet("/applications", (IFeatureRepository featureRepository) =>
    {
        apiLogger.LogInformation("Listing applications");
        var featureService = new FeatureService(featureRepository);

        try
        {
            var apps = featureService.GetApplications();
            apiLogger.LogInformation("Returning {Count} application(s)", apps.Count);
            return Results.Ok(apps);
        }
        catch (IncorrectEnvironmentException exception)
        {
            apiLogger.LogError(exception, "Incorrect environment while listing applications");
            return Results.BadRequest(new BadHttpRequestException(exception.Message));
        }
    })
    .WithDescription("Used by manager to list applications.")
    .WithTags("Manager");

app.MapGet("/applications/{applicationName}/features/", (string applicationName, IFeatureRepository featureRepository) =>
    {
        apiLogger.LogInformation("Listing features for app {AppName}", applicationName);
        var featureService = new FeatureService(featureRepository);

        try
        {
            var features = featureService.GetFeaturesForApplication(new ApplicationDto(applicationName));
            apiLogger.LogInformation("Returning {Count} feature(s) for app {AppName}", features.Count, applicationName);
            return Results.Ok(features);
        }
        catch (ApplicationNotFoundException exception)
        {
            apiLogger.LogWarning("App {AppName} not found while listing features: {Message}", applicationName, exception.Message);
            return Results.BadRequest(new BadHttpRequestException(exception.Message));
        }
    })
    .WithDescription("Used by manager to list application features.")
    .WithTags("Manager");

app.MapGet("/applications/{applicationName}/features/{featureName}/state/", (string applicationName, string featureName, IFeatureRepository featureRepository) =>
    {
        apiLogger.LogInformation("Getting state of feature {FeatureName} for app {AppName}", featureName, applicationName);
        var featureService = new FeatureService(featureRepository);
        try
        {
            var state = featureService.GetFeatureState(new ApplicationDto(applicationName), featureName);
            apiLogger.LogInformation("Feature {FeatureName} in app {AppName} is {State}", featureName, applicationName, state);
            return Results.Ok(state);
        }
        catch (FeatureNotFoundException)
        {
            apiLogger.LogWarning("Feature {FeatureName} not found in app {AppName}", featureName, applicationName);
            return Results.NotFound();
        }
    })
    .WithDescription("Used to check flag state on client.")
    .WithTags("Client")
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest);

app.MapPut("/applications/{applicationName}/features/{featureName}",
        (string applicationName, string featureName, FeatureStateModel featureState, IFeatureRepository featureRepository) =>
        {
            apiLogger.LogInformation("Setting feature {FeatureName} in app {AppName} to {State}", featureName, applicationName, featureState.State);
            var featureService = new FeatureService(featureRepository);
            try
            {
                var feature = new FeatureDto(featureName, featureState.State);
                featureService.UpdateFeature(new ApplicationDto(applicationName), feature);
                apiLogger.LogInformation("Feature {FeatureName} in app {AppName} updated to {State}", featureName, applicationName, featureState.State);
                return Results.Ok();
            }
            catch (FeatureNotFoundException)
            {
                apiLogger.LogWarning("Feature {FeatureName} not found in app {AppName} while updating state", featureName, applicationName);
                return Results.NotFound();
            }
        })
    .WithDescription("Used by manager to set flag state.")
    .WithTags("Manager")
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

app.Run();