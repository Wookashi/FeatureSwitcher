using System.IO.Compression;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.ResponseCompression;
using Swashbuckle.AspNetCore.SwaggerUI;
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

app.MapPut("/api/nodes", (NodeRegistrationModel nodeRegistrationModel, INodeRepository nodeRepository) =>
    {
        var nodeService = new NodeService(nodeRepository);
        nodeService.CreateOrReplaceNode(nodeRegistrationModel);
        return Results.Created();
    })
    .WithName("Register Node on Manager");


app.MapFallbackToFile("index.html");

app.Run();