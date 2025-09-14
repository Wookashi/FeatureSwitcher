using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Api.CustomHealthChecks;
using Wookashi.FeatureSwitcher.Node.Api.Models;
using Wookashi.FeatureSwitcher.Node.Api.Services;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var dbConnectionString = builder.Configuration["NodeConfiguration:ConnectionString"] ?? string.Empty;
builder.Services.AddDatabase(dbConnectionString);
builder.Services.AddHealthChecks()
    .AddCheck<ManagerHealthCheck>("Manager Api");

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

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new 
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                data = entry.Value.Data
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapPost("/applications", (ApplicationRegistrationRequestModel registerModel, IFeatureRepository featureRepository) =>
    {
        if (registerModel.Environment != environment)
        {
            return Results.BadRequest(new BadHttpRequestException("Environment does not match"));
        }

        var featureService = new FeatureService(featureRepository);

        try
        {
            featureService.RegisterApplication(new ApplicationDto(registerModel.AppName, registerModel.Environment), registerModel.Features);           
        }
        catch (IncorrectEnvironmentException exception)
        {
            return Results.BadRequest(new BadHttpRequestException(exception.Message));
        }

        return Results.Created();
    })
    .WithName("Register Application and Features")
    .WithOpenApi(operation => new OpenApiOperation(operation)
    {
        Summary = "Allow register current used features in specific app",
        Description = "Client can provide all possible features it can serve"
    });
    
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
    .WithName("Applications")
    .WithOpenApi(operation => new OpenApiOperation(operation)
    {
        Summary = "List all registered apps",
        Description = "List all registered apps"
    });

app.MapGet("/applications/{applicationName}/features/", (string applicationName, IFeatureRepository featureRepository) =>
{
    var featureService = new FeatureService(featureRepository);

    try
    {
        return Results.Ok(featureService.GetFeaturesForApplication(new ApplicationDto(applicationName, environment)));
    }
    catch (ApplicationNotFoundException exception)
    {
        return Results.BadRequest(new BadHttpRequestException(exception.Message));
    }
})
.WithName("Features")
.WithOpenApi(operation => new OpenApiOperation(operation)
{
    Summary = "List all features",
    Description = "List all features with states for application"
});

app.MapGet("/applications/{applicationName}/features/{featureName}/state/", (string applicationName, string featureName, IFeatureRepository featureRepository) =>
    {
        var featureService = new FeatureService(featureRepository);
        try
        {
            return Results.Ok(featureService.GetFeatureState(new ApplicationDto(applicationName, environment), featureName));           
        }
        catch (FeatureNotFoundException)
        {
            return Results.NotFound();
        }
    })
    .WithName("GetFeatureState")
    .WithOpenApi(operation => new OpenApiOperation(operation)
    {
        Summary = "Allow checking feature state in real time",
        Description = "Client can provide feature name and in response is feature state information"
    });

app.MapPut("/applications/{applicationName}/features/{featureName}", (string applicationName, string featureName, FeatureStateDto featureState, IFeatureRepository featureRepository) =>
    {
        var featureService = new FeatureService(featureRepository);
        try
        {
            var enabled = featureState.State;
            var feature = new FeatureDto(featureName, enabled);
            featureService.UpdateFeature(new ApplicationDto(applicationName, environment), feature);
            return Results.Ok();           
        }
        catch (FeatureNotFoundException)
        {
            return Results.NotFound();
        }
    })
    .WithName("SetFeatureState")
    .WithOpenApi(operation => new OpenApiOperation(operation)
    {
        Summary = "Allow setting feature state",
        Description = "Client can provide app, feature name and state"
    });

app.Run();