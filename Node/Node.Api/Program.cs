using Microsoft.OpenApi.Models;
using Wookashi.FeatureSwitcher.Node.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//TODO Temporary solution
//TODO Add to settings or system variables
var _environment = "testEnvironment";
var featuresList = new List<FeatureStateDto>
{
    new("someAppName","testEnvironment","someFeatureForTestPurposes", true),
    new("someOtherAppName","testOtherEnvironment","someFeatureForTestPurposes2", true)

};

// END TODO

app.MapGet("/{applicationName}/{featureName}/state/", (HttpRequest request, string applicationName , string featureName) =>
    {
        //todo return notfoundcode
        return true;
    })
    .WithName("GetFeatureState")
    .WithOpenApi(operation => new OpenApiOperation(operation)
    {
        Summary = "Allow checking feature state in real time",
        Description = "Client can provide feature name and in response is feature state information"
    });

app.MapPost("/features/register", (RegisterFeaturesRequestModel registerModel) =>
    {
        //TODO check environment
        if (registerModel.Environment != _environment)
        {
            return Results.BadRequest(new BadHttpRequestException("Environment does not match"));
        }
        
       // var appFeatures = featuresList.Where(feature => feature.Environment == registerModel.Environment && feature.AppName == registerModel.AppName).ToList();
        var appFeatures = featuresList
            .Where(feature => feature.Environment == registerModel.Environment && feature.AppName == registerModel.AppName)
            .ToList();

        var featuresToDelete = appFeatures
            .Where(feature => !registerModel.Features.Select(f => f.FeatureName)
                .Contains(feature.FeatureName)).ToList();
        
        var featuresToAdd = registerModel.Features
            .Where(feature => !appFeatures.Select(f => f.FeatureName)
                .Contains(feature.FeatureName)).ToList();
            
        featuresList.AddRange(featuresToAdd
            .Select(feature => new FeatureStateDto(
                registerModel.AppName, 
                registerModel.Environment,
                feature.FeatureName,
                feature.InitialState))
            .ToList());

        foreach (var feature in featuresToDelete)
        {
            var featureToDelete = featuresList.FirstOrDefault(ftr => ftr.FeatureName == feature.FeatureName);
            if (featureToDelete == null) continue;
            featuresList.Remove(featureToDelete);
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