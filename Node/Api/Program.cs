using Microsoft.OpenApi;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;
using Wookashi.FeatureSwitcher.Node.Api.HealthChecks;
using Wookashi.FeatureSwitcher.Node.Api.Models;
using Wookashi.FeatureSwitcher.Node.Api.Services;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<ManagerSettings>(
    builder.Configuration.GetSection("ManagerSettings"));
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

var app = builder.Build();

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

var environment = builder.Configuration["NodeConfiguration:Environment"] ?? "FailConfigurationSetting";

app.UseHealthCheck();

app.MapPost("/applications",
        (ApplicationRegistrationRequestModel registerModel, IFeatureRepository featureRepository) =>
        {
            if (registerModel.Environment != environment)
            {
                return Results.BadRequest(new BadHttpRequestException("Environment does not match"));
            }

            var featureService = new FeatureService(featureRepository);

            featureService.RegisterApplication(new ApplicationDto(registerModel.AppName),
                registerModel.Features);


            return Results.Created();
        })
    .WithDescription("Used to register application by app client. Adds or updates app data in node database.")
    .WithTags("Client");

app.MapGet("/applications", (IFeatureRepository featureRepository) =>
    {
        var featureService = new FeatureService(featureRepository);

        try
        {
            return Results.Ok(featureService.GetApplications());
        }
        catch (IncorrectEnvironmentException exception)
        {
            return Results.BadRequest(new BadHttpRequestException(exception.Message));
        }
    })
    .WithDescription("Used by manager to list applications.")
    .WithTags("Manager");

app.MapGet("/applications/{applicationName}/features/", (string applicationName, IFeatureRepository featureRepository) =>
    {
        var featureService = new FeatureService(featureRepository);

        try
        {
            return Results.Ok(featureService.GetFeaturesForApplication(new ApplicationDto(applicationName)));
        }
        catch (ApplicationNotFoundException exception)
        {
            return Results.BadRequest(new BadHttpRequestException(exception.Message));
        }
    })
    .WithDescription("Used by manager to list application features.")
    .WithTags("Manager");

app.MapGet("/applications/{applicationName}/features/{featureName}/state/", (string applicationName, string featureName, IFeatureRepository featureRepository) =>
    {
        var featureService = new FeatureService(featureRepository);
        try
        {
            return Results.Ok(featureService.GetFeatureState(new ApplicationDto(applicationName), featureName));
        }
        catch (FeatureNotFoundException)
        {
            return Results.NotFound();
        }
    })
    .WithDescription("Used to check flag state on client.")
    .WithTags("Client");

app.MapPut("/applications/{applicationName}/features/{featureName}",
        (string applicationName, string featureName, FeatureStateDto featureState, IFeatureRepository featureRepository) =>
        {
            var featureService = new FeatureService(featureRepository);
            try
            {
                var enabled = featureState.State;
                var feature = new FeatureDto(featureName, enabled);
                featureService.UpdateFeature(new ApplicationDto(applicationName), feature);
                return Results.Ok();
            }
            catch (FeatureNotFoundException)
            {
                return Results.NotFound();
            }
        })
    .WithName("SetFeatureState");

app.Run();