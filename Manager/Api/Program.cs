using System.IO.Compression;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Api.Configuration;
using Wookashi.FeatureSwitcher.Manager.Api.Extensions;
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
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NodeAccessService>();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("EditorOrAdmin", p => p.RequireRole("Admin", "Editor"));
});

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

static UserResponse ToUserResponse(UserDto dto) => new()
{
    Id = dto.Id,
    Username = dto.Username,
    Role = dto.Role,
    CreatedAt = dto.CreatedAt,
    UpdatedAt = dto.UpdatedAt,
    AccessibleNodeIds = dto.AccessibleNodeIds,
};

app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTimeOffset.UtcNow })).ExcludeFromDescription();

// --- Auth endpoints ---

app.MapGet("/api/auth/setup-required", (IUserRepository userRepository) =>
    {
        return Results.Ok(new { required = !userRepository.AnyUsersExist() });
    })
    .AllowAnonymous()
    .WithDescription("Check if initial setup is required.");

app.MapPost("/api/auth/setup", (SetupRequest request, IUserRepository userRepository, AuthService authService, IAuditLogRepository auditLog) =>
    {
        if (userRepository.AnyUsersExist())
            return Results.Conflict(new { error = "Setup already completed. Users already exist." });

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { error = "Username and password are required." });

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        userRepository.CreateUser(request.Username.Trim(), hash, "Admin", []);

        auditLog.AddEntry(request.Username.Trim(), "Setup", "Initial admin account created");

        var (token, expiresAt, role) = authService.GenerateToken(request.Username.Trim());
        return Results.Ok(new LoginResponse { Token = token, ExpiresAt = expiresAt, Role = role });
    })
    .AllowAnonymous()
    .WithDescription("Create the first admin account (initial setup).");

app.MapPost("/api/auth/login", (LoginRequest request, AuthService authService) =>
    {
        if (!authService.ValidateCredentials(request.Username, request.Password))
        {
            return Results.Unauthorized();
        }

        var (token, expiresAt, role) = authService.GenerateToken(request.Username);
        return Results.Ok(new LoginResponse { Token = token, ExpiresAt = expiresAt, Role = role });
    })
    .AllowAnonymous()
    .WithDescription("Authenticate and obtain a JWT token.");

app.MapGet("/api/auth/me", (ClaimsPrincipal user, IUserRepository userRepository) =>
    {
        var userId = user.GetUserId();
        var dto = userRepository.GetUserById(userId);
        if (dto is null) return Results.NotFound();
        return Results.Ok(ToUserResponse(dto));
    })
    .RequireAuthorization()
    .WithDescription("Get current user info.");

app.MapPut("/api/auth/password", (ChangePasswordRequest request, ClaimsPrincipal user,
        IUserRepository userRepository, IAuditLogRepository auditLog) =>
    {
        var userId = user.GetUserId();
        var username = user.GetUserName();

        var hash = userRepository.GetPasswordHash(username);
        if (hash is null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, hash))
            return Results.BadRequest(new { error = "Current password is incorrect." });

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return Results.BadRequest(new { error = "New password is required." });

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        userRepository.UpdatePassword(userId, newHash);
        auditLog.AddEntry(username, "PasswordChange", "User changed their password");

        return Results.Ok(new { message = "Password changed successfully." });
    })
    .RequireAuthorization()
    .WithDescription("Change own password.");

// --- User management endpoints (Admin only) ---

app.MapGet("/api/users", (IUserRepository userRepository) =>
    {
        return Results.Ok(userRepository.GetAllUsers().Select(ToUserResponse).ToList());
    })
    .RequireAuthorization("AdminOnly")
    .WithDescription("List all users.");

app.MapGet("/api/users/{id:int}", (int id, IUserRepository userRepository) =>
    {
        var dto = userRepository.GetUserById(id);
        if (dto is null) return Results.NotFound();
        return Results.Ok(ToUserResponse(dto));
    })
    .RequireAuthorization("AdminOnly")
    .WithDescription("Get user by ID.");

app.MapPost("/api/users", (CreateUserRequest request, IUserRepository userRepository,
        ClaimsPrincipal user, IAuditLogRepository auditLog) =>
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { error = "Username and password are required." });

        if (request.Role is not ("Admin" or "Editor" or "Viewer"))
            return Results.BadRequest(new { error = "Role must be Admin, Editor, or Viewer." });

        if (userRepository.GetUserByUsername(request.Username.Trim()) is not null)
            return Results.Conflict(new { error = "A user with this username already exists." });

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var dto = userRepository.CreateUser(request.Username.Trim(), hash, request.Role, request.NodeIds);

        var adminUsername = user.GetUserName();
        
        
        auditLog.AddEntry(adminUsername, "CreateUser", $"Created user '{dto.Username}' with role {dto.Role}");

        return Results.Created($"/api/users/{dto.Id}", ToUserResponse(dto));
    })
    .RequireAuthorization("AdminOnly")
    .WithDescription("Create a new user.");

app.MapPut("/api/users/{id:int}", (int id, UpdateUserRequest request, IUserRepository userRepository,
        ClaimsPrincipal user, IAuditLogRepository auditLog) =>
    {
        var existing = userRepository.GetUserById(id);
        if (existing is null) return Results.NotFound();

        if (request.Role is not null and not ("Admin" or "Editor" or "Viewer"))
            return Results.BadRequest(new { error = "Role must be Admin, Editor, or Viewer." });

        var dto = userRepository.UpdateUser(id, request.Role, request.NodeIds);

        var adminUsername = user.GetUserName();
        auditLog.AddEntry(adminUsername, "UpdateUser", $"Updated user '{dto.Username}'");

        return Results.Ok(ToUserResponse(dto));
    })
    .RequireAuthorization("AdminOnly")
    .WithDescription("Update user role and node access.");

app.MapDelete("/api/users/{id:int}", (int id, IUserRepository userRepository,
        ClaimsPrincipal user, IAuditLogRepository auditLog) =>
    {
        var currentUserId = user.GetUserId();
        var adminUsername = user.GetUserName();
        if (currentUserId == id)
            return Results.BadRequest(new { error = "Cannot delete your own account." });

        var existing = userRepository.GetUserById(id);
        if (existing is null) return Results.NotFound();

        userRepository.DeleteUser(id);
        auditLog.AddEntry(adminUsername, "DeleteUser", $"Deleted user '{existing.Username}'");

        return Results.NoContent();
    })
    .RequireAuthorization("AdminOnly")
    .WithDescription("Delete a user.");

// --- Audit log endpoint ---

app.MapGet("/api/audit-log", ([FromQuery] int count, [FromQuery] int offset, IAuditLogRepository auditLog) =>
    {
        if (count <= 0) count = 50;
        if (offset < 0) offset = 0;
        return Results.Ok(auditLog.GetRecentEntries(count, offset));
    })
    .RequireAuthorization("AdminOnly")
    .WithDescription("View audit log entries.");

// --- Node endpoints ---

app.MapGet("/api/nodes", (INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory,
        ClaimsPrincipal user, NodeAccessService nodeAccessService) =>
    {
        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        var allNodes = nodeService.GetAllNodes();

        var userId = user.GetUserId();
        var role = user.GetUserRole();
        
        if (role == "Admin")
            return Results.Ok(allNodes);

        var accessibleIds = nodeAccessService.GetAccessibleNodeIds(userId, role);
        var filtered = allNodes.Where(n => accessibleIds.Contains(n.Id)).ToList();
        return Results.Ok(filtered);
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
    .RequireAuthorization("AdminOnly")
    .WithDescription("Used to register node. Adds or updates node data in manager database.");

app.MapGet("/api/nodes/{nodeId:int}/applications", async (int nodeId, INodeRepository nodeRepository,
        [FromServices] IHttpClientFactory httpClientFactory, ClaimsPrincipal user, NodeAccessService nodeAccessService) =>
    {
        var userId = user.GetUserId();
        var role = user.GetUserRole();
        
        if (!nodeAccessService.CanAccessNode(userId, role, nodeId))
            return Results.Forbid();

        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        var apps = await nodeService.GetApplicationsAsync(nodeId);
        return Results.Ok(apps);
    })
    .RequireAuthorization()
    .WithDescription("Used to list application on node.");

app.MapGet("/api/nodes/{nodeId:int}/applications/{appName}/features", async (int nodeId, string appName,
                                INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory,
                                ClaimsPrincipal user, NodeAccessService nodeAccessService) =>
    {
        var userId = user.GetUserId();
        var role = user.GetUserRole();
        if (!nodeAccessService.CanAccessNode(userId, role, nodeId))
            return Results.Forbid();

        var nodeService = new NodeService(nodeRepository, httpClientFactory);
        var features = await nodeService.GetFeaturesForApplicationAsync(nodeId, appName);
        return Results.Ok(features);
    })
    .RequireAuthorization()
    .WithDescription("Used to list features for application on node.");

app.MapPut("/api/nodes/{nodeId:int}/applications/{appName}/features/{featureName}", async (int nodeId, string appName,
string featureName, FeatureStateModel featureState, INodeRepository nodeRepository, [FromServices] IHttpClientFactory httpClientFactory,
ClaimsPrincipal user, NodeAccessService nodeAccessService, IAuditLogRepository auditLog) =>
    {
        var userId = user.GetUserId();
        var username = user.GetUserName();
        var role = user.GetUserRole();

        if (!nodeAccessService.CanAccessNode(userId, role, nodeId))
            return Results.Forbid();

        featureState.ChangedBy = username;

        var nodeService = new NodeService(nodeRepository, httpClientFactory);

        var response = await nodeService.SetFeatureStateAsync(nodeId, appName, featureName, featureState);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            auditLog.AddEntry(username, "ToggleFeature",
                $"{featureName} in {appName} on node {nodeId} set to {featureState.State}");
        }

        return response.StatusCode switch
        {
            HttpStatusCode.OK => Results.Ok(),
            HttpStatusCode.BadRequest => Results.BadRequest(),
            _ => Results.InternalServerError()
        };
    })
    .RequireAuthorization("EditorOrAdmin")
    .WithDescription("Used to change feature state on node.");


app.MapFallbackToFile("index.html");

app.Run();
