using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;
using Wookashi.FeatureSwitcher.Node.Api.HealthChecks;
using Wookashi.FeatureSwitcher.Node.Api.Models;
using Wookashi.FeatureSwitcher.Node.Api.Services;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Logger;
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
builder.Services.AddHostedService<SoftDeleteSweepHostedService>();

//Console logs configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsoleFormatter<MinimalConsoleFormatter, ConsoleFormatterOptions>();
builder.Logging.AddConsole(options => {
    options.FormatterName = "minimal";
});

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (dbConnectionString != string.Empty)
{
    app.MigrateDatabase(logger);
}

app.UseHttpsRedirection();

app.UseHealthCheck(
    nodeConfig?.Name ?? "(not set)",
    nodeEnvironment,
    nodeConfig?.Address ?? "(not set)");

app.MapPost("/applications",
        (ApplicationRegistrationRequestModel registerModel, IFeatureRepository featureRepository) =>
        {
            logger.LogDebug("Registering app {AppName} for environment {Environment} with {FeatureCount} feature(s)",
                registerModel.AppName, registerModel.Environment, registerModel.Features?.Count ?? 0);

            if (registerModel.Environment != nodeEnvironment)
            {
                logger.LogError("Environment mismatch: node is '{NodeEnv}', request is '{ReqEnv}' for app {AppName}",
                    nodeEnvironment, registerModel.Environment, registerModel.AppName);
                return Results.Problem(
                    title: "Environment mismatch",
                    detail: $"Node environment is '{nodeEnvironment}', but request environment is '{registerModel.Environment}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            var featureService = new FeatureService(featureRepository);
            featureService.RegisterApplication(new ApplicationDto(registerModel.AppName), registerModel.Features ?? []);

            logger.LogInformation("App {AppName} registered successfully", registerModel.AppName);
            return Results.Created();
        })
    .WithDescription("Used to register application by app client. Adds or updates app data in node database.")
    .WithTags("Client")
    .Produces(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status409Conflict);

app.MapGet("/applications", (IFeatureRepository featureRepository) =>
    {
        logger.LogDebug("Listing applications");
        var featureService = new FeatureService(featureRepository);

        try
        {
            var apps = featureService.GetApplications();
            logger.LogDebug("Returning {Count} application(s)", apps.Count);
            return Results.Ok(apps);
        }
        catch (IncorrectEnvironmentException exception)
        {
            logger.LogError(exception, "Incorrect environment while listing applications");
            return Results.BadRequest(new BadHttpRequestException(exception.Message));
        }
    })
    .WithDescription("Used by manager to list applications.")
    .WithTags("Manager");

app.MapGet("/applications/{applicationName}/features/", (string applicationName, IFeatureRepository featureRepository) =>
    {
        logger.LogDebug("Listing features (with usage) for app {AppName}", applicationName);

        try
        {
            var features = featureRepository.GetFeaturesWithUsageForApplication(new ApplicationDto(applicationName));
            logger.LogDebug("Returning {Count} feature(s) for app {AppName}", features.Count, applicationName);
            return Results.Ok(features);
        }
        catch (ApplicationNotFoundException exception)
        {
            logger.LogWarning("App {AppName} not found while listing features: {Message}", applicationName, exception.Message);
            return Results.BadRequest(new BadHttpRequestException(exception.Message));
        }
    })
    .WithDescription("Used by manager to list application features with usage metadata (LastUsedAt, UsesLast7Days).")
    .WithTags("Manager");

app.MapGet("/applications/{applicationName}/features/{featureName}/state/", (string applicationName, string featureName, IFeatureRepository featureRepository) =>
    {
        logger.LogDebug("Getting state of feature {FeatureName} for app {AppName}", featureName, applicationName);
        var featureService = new FeatureService(featureRepository);
        try
        {
            var state = featureService.GetFeatureState(new ApplicationDto(applicationName), featureName);
            logger.LogDebug("Feature {FeatureName} in app {AppName} is {State}", featureName, applicationName, state);
            return Results.Ok(state);
        }
        catch (FeatureNotFoundException)
        {
            logger.LogWarning("Feature {FeatureName} not found in app {AppName}", featureName, applicationName);
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
            logger.LogDebug("Setting feature {FeatureName} in app {AppName} to {State}", featureName, applicationName, featureState.State);
            var featureService = new FeatureService(featureRepository);
            try
            {
                var feature = new FeatureDto(featureName, featureState.State);
                var result = featureService.UpdateFeature(new ApplicationDto(applicationName), feature);
                logger.LogInformation(
                    "Feature {FeatureName} updated to {State} from app {AppName}; affected active application(s): {AffectedApplications}",
                    featureName, featureState.State, applicationName, result.AffectedApplications);
                return Results.Ok(result);
            }
            catch (FeatureNotFoundException)
            {
                logger.LogWarning("Feature {FeatureName} not found in app {AppName} while updating state", featureName, applicationName);
                return Results.NotFound();
            }
        })
    .WithDescription("Used by manager to set flag state.")
    .WithTags("Manager")
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

app.MapGet("/pending-deletion/features", (IFeatureRepository featureRepository) =>
    {
        logger.LogDebug("Listing features pending deletion");
        var featureService = new FeatureService(featureRepository);
        var pending = featureService.GetPendingFeatures();
        return Results.Ok(pending);
    })
    .WithDescription("Lists features currently soft-deleted (pending permanent deletion). Used by manager UI.")
    .WithTags("Manager");

app.MapGet("/pending-deletion/applications", (IFeatureRepository featureRepository) =>
    {
        logger.LogDebug("Listing applications pending deletion");
        var featureService = new FeatureService(featureRepository);
        var pending = featureService.GetPendingApplications();
        return Results.Ok(pending);
    })
    .WithDescription("Lists applications currently soft-deleted (pending permanent deletion). Used by manager UI.")
    .WithTags("Manager");

app.MapDelete("/applications/{applicationName}/features/{featureName}/pending",
        (string applicationName, string featureName, IFeatureRepository featureRepository) =>
        {
            logger.LogDebug("Permanently deleting feature {FeatureName} in app {AppName}", featureName, applicationName);
            var featureService = new FeatureService(featureRepository);
            try
            {
                var result = featureService.PermanentlyDeleteFeature(applicationName, featureName);
                logger.LogInformation(
                    "Feature {FeatureName} in app {AppName} permanently deleted (last used {LastUsedAt:o}, pending since {PendingSince:o})",
                    featureName, applicationName, result.LastUsedAt, result.PendingDeletionSince);
                return Results.Ok(result);
            }
            catch (FeatureNotFoundException)
            {
                logger.LogWarning("Feature {FeatureName} not found in app {AppName} during permanent deletion", featureName, applicationName);
                return Results.NotFound();
            }
            catch (FeatureNotPendingDeletionException ex)
            {
                logger.LogInformation(
                    "Permanent deletion of {FeatureName} in {AppName} skipped — feature was restored: {Message}",
                    featureName, applicationName, ex.Message);
                return Results.Conflict(new { ex.Message });
            }
        })
    .WithDescription("Permanently deletes a feature that is in PendingDeletion state. Returns 409 if the feature was restored between view and confirm.")
    .WithTags("Manager")
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

app.MapDelete("/applications/{applicationName}/pending",
        (string applicationName, IFeatureRepository featureRepository) =>
        {
            logger.LogDebug("Permanently deleting app {AppName}", applicationName);
            var featureService = new FeatureService(featureRepository);
            try
            {
                var result = featureService.PermanentlyDeleteApplication(applicationName);
                logger.LogInformation(
                    "Application {AppName} permanently deleted (last used {LastUsedAt:o}, pending since {PendingSince:o})",
                    applicationName, result.LastUsedAt, result.PendingDeletionSince);
                return Results.Ok(result);
            }
            catch (ApplicationNotFoundException)
            {
                logger.LogWarning("Application {AppName} not found during permanent deletion", applicationName);
                return Results.NotFound();
            }
            catch (FeatureNotPendingDeletionException ex)
            {
                logger.LogInformation(
                    "Permanent deletion of app {AppName} skipped — application was restored: {Message}",
                    applicationName, ex.Message);
                return Results.Conflict(new { ex.Message });
            }
        })
    .WithDescription("Permanently deletes an application (and its features) that is in PendingDeletion state. Returns 409 if it was restored between view and confirm.")
    .WithTags("Manager")
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

app.Run();
