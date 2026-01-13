using System.IO.Compression;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Api.Services;
using Wookashi.FeatureSwitcher.Manager.Database.Extensions;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var dbConnectionString = builder.Configuration["Database:ConnectionString"] ?? string.Empty;
builder.Services.AddDatabase(dbConnectionString);

builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.Providers.Add<GzipCompressionProvider>();
    opts.Providers.Add<BrotliCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddHttpClient();

var app = builder.Build();

if (dbConnectionString != string.Empty)
{
    app.MigrateDatabase();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/hello", () => Results.Ok(new { message = "Hello from .NET 9" }));
app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTimeOffset.UtcNow })).ExcludeFromDescription();

app.MapGet("/api/nodes", (INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        return Results.Ok(nodeService.GetAllNodes());
    })
    .WithDescription("Used to list nodes.");
app.MapPut("/api/nodes", (NodeRegistrationModel nodeRegistrationModel, 
                                    INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        nodeService.CreateOrReplaceNode(nodeRegistrationModel);
        return Results.Created();
    })
    .WithDescription("Used to register node. Adds or updates node data in manager database.");

app.MapGet("/api/nodes/{nodeId:int}/applications", async (int nodeId, INodeRepository nodeRepository,
        [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        var apps = await nodeService.GetApplicationsAsync(nodeId);
        return Results.Ok(apps);
    })
    .WithDescription("Used to list application on node.");

app.MapGet("/api/nodes/{nodeId:int}/applications/{appName}/features", async (int nodeId, string appName,
                                INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        var features = await nodeService.GetFeaturesForApplicationAsync(nodeId, appName);
        return Results.Ok(features);
    })
    .WithDescription("Used to list features for application on node.");

app.MapPut("/api/nodes/{nodeId:int}/applications/{appName}/features/{featureName}", async (int nodeId, string appName,
string featureName, FeatureStateModel featureState, INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        
        var response = await nodeService.SetFeatureStateAsync(nodeId, appName, featureName, featureState);
        return response.StatusCode switch
        {
            HttpStatusCode.OK => Results.Ok(),
            HttpStatusCode.BadRequest => Results.BadRequest(),
            _ => Results.InternalServerError()
        };
    })
    .WithDescription("Used to change feature state on node.");


app.MapFallbackToFile("index.html");

app.Run();