using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Database.Extensions;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Dtos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var dbConnectionString = builder.Configuration["NodeConfiguration:ConnectionString"] ?? string.Empty;
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
app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTimeOffset.UtcNow }));

app.MapPost("/nodes", (NodeRegistrationModel registerModel, IFeatureStatesRepository featureStateRepository) =>
    {
        // var featureService = new FeatureService(featureRepository);
        //
        // try
        // {
        //     featureService.RegisterApplication(new ApplicationDto(registerModel.AppName, registerModel.Environment), registerModel.Features);           
        // }
        // catch (IncorrectEnvironmentException exception)
        // {
        //     return Results.BadRequest(new BadHttpRequestException(exception.Message));
        // }

        return Results.Created();
    })
    .WithName("Register Application and Features");


app.MapFallbackToFile("index.html");

app.Run();