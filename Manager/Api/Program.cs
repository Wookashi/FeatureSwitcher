using System.IO.Compression;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Api.Configuration;
using Wookashi.FeatureSwitcher.Manager.Api.Models;
using Wookashi.FeatureSwitcher.Manager.Api.Services;
using Wookashi.FeatureSwitcher.Manager.Database.Extensions;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

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

// JWT configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AdminCredentials>(builder.Configuration.GetSection("AdminCredentials"));
builder.Services.AddSingleton<AuthService>();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTimeOffset.UtcNow })).ExcludeFromDescription();

app.MapPost("/api/auth/login", (LoginRequest request, AuthService authService) =>
    {
        if (!authService.ValidateCredentials(request.Username, request.Password))
        {
            return Results.Unauthorized();
        }

        var (token, expiresAt) = authService.GenerateToken();
        return Results.Ok(new LoginResponse { Token = token, ExpiresAt = expiresAt });
    })
    .AllowAnonymous()
    .WithDescription("Authenticate and obtain a JWT token.");

app.MapGet("/api/nodes", (INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        return Results.Ok(nodeService.GetAllNodes());
    })
    .RequireAuthorization()
    .WithDescription("Used to list nodes.");
app.MapPut("/api/nodes", (NodeRegistrationModel nodeRegistrationModel,
                                    INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        nodeService.CreateOrReplaceNode(nodeRegistrationModel);
        return Results.Created();
    })
    .RequireAuthorization()
    .WithDescription("Used to register node. Adds or updates node data in manager database.");

app.MapGet("/api/nodes/{nodeId:int}/applications", async (int nodeId, INodeRepository nodeRepository,
        [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        var apps = await nodeService.GetApplicationsAsync(nodeId);
        return Results.Ok(apps);
    })
    .RequireAuthorization()
    .WithDescription("Used to list application on node.");

app.MapGet("/api/nodes/{nodeId:int}/applications/{appName}/features", async (int nodeId, string appName,
                                INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        var features = await nodeService.GetFeaturesForApplicationAsync(nodeId, appName);
        return Results.Ok(features);
    })
    .RequireAuthorization()
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
    .RequireAuthorization()
    .WithDescription("Used to change feature state on node.");


app.MapFallbackToFile("index.html");

app.Run();
