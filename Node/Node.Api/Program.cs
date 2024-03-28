using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Models;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Api.Models;
using Wookashi.FeatureSwitcher.Node.Api.Services;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;
using Wookashi.FeatureSwitcher.Node.Database.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDatabase();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//TODO Add to settings or system variables
const string environment = "testEnvironment";
// END TODO

app.MapGet("/{applicationName}/{featureName}/state/", (string applicationName, string featureName, IFeatureRepository featureRepository) =>
    {
        var featureService = new FeatureService(featureRepository, new ApplicationDto(applicationName, environment));
        try
        {
            return Results.Ok(featureService.GetFeatureState(featureName));           
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

app.MapPost("/features/register", (RegisterFeaturesRequestModel registerModel, IFeatureRepository featureRepository) =>
    {
        if (registerModel.Environment != environment)
        {
            return Results.BadRequest(new BadHttpRequestException("Environment does not match"));
        }

        var featureService = new FeatureService(featureRepository, new ApplicationDto(registerModel.AppName, registerModel.Environment));

        try
        {
            featureService.RegisterFeatures(registerModel.Features);           
        }
        catch (IncorrectEnvironmentException exception)
        {
            return Results.BadRequest(new BadHttpRequestException(exception.Message));
        }
        return Results.Ok();
    })
    .WithName("RegisterFeatures")
    .WithOpenApi(operation => new OpenApiOperation(operation)
    {
        Summary = "Allow register current used features in specific app",
        Description = "Client can provide all possible features it can serve"
    });

app.Run();