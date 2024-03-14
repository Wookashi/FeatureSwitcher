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

app.MapGet("/features/{name}/state", (HttpRequest request) =>
    {
        var featureName = request.RouteValues["name"];
        return true;
    })
    .WithName("GetFeatureState")
    .WithOpenApi(operation => new(operation)
    {
        Summary = "Allow checking feature state in real time",
        Description = "Client can provide feature name and in response is feature state information"
    });

app.MapPost("/features/register", (RegisterFeaturesRequestModel registerModel) =>
    {
        //save featres state and register it.
        return true;
    }

app.Run();