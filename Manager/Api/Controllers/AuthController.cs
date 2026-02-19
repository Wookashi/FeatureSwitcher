using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Api.Extensions;
using Wookashi.FeatureSwitcher.Manager.Api.Models;
using Wookashi.FeatureSwitcher.Manager.Api.Services;

namespace Wookashi.FeatureSwitcher.Manager.Api.Controllers;

[ApiController]
[Route("api/auth")]
internal class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly AuthService _authService;
    private readonly IAuditLogRepository _auditLog;

    public AuthController(IUserRepository userRepository, AuthService authService, IAuditLogRepository auditLog)
    {
        _userRepository = userRepository;
        _authService = authService;
        _auditLog = auditLog;
    }

    [HttpGet("setup-required")]
    [AllowAnonymous]
    public IActionResult SetupRequired()
    {
        return Ok(new { required = !_userRepository.AnyUsersExist() });
    }

    [HttpPost("setup")]
    [AllowAnonymous]
    public IActionResult Setup([FromBody] SetupRequest request)
    {
        if (_userRepository.AnyUsersExist())
            return Conflict(new { error = "Setup already completed. Users already exist." });

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Username and password are required." });

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        _userRepository.CreateUser(request.Username.Trim(), hash, "Admin", []);

        _auditLog.AddEntry(request.Username.Trim(), "Setup", "Initial admin account created");

        var (token, expiresAt, role) = _authService.GenerateToken(request.Username.Trim());
        return Ok(new LoginResponse { Token = token, ExpiresAt = expiresAt, Role = role });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!_authService.ValidateCredentials(request.Username, request.Password))
        {
            return Unauthorized();
        }

        var (token, expiresAt, role) = _authService.GenerateToken(request.Username);
        return Ok(new LoginResponse { Token = token, ExpiresAt = expiresAt, Role = role });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.GetUserId();
        var dto = _userRepository.GetUserById(userId);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPut("password")]
    [Authorize]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.GetUserId();
        var username = User.GetUserName();

        var hash = _userRepository.GetPasswordHash(username);
        if (hash is null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, hash))
            return BadRequest(new { error = "Current password is incorrect." });

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { error = "New password is required." });

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        _userRepository.UpdatePassword(userId, newHash);
        _auditLog.AddEntry(username, "PasswordChange", "User changed their password");

        return Ok(new { message = "Password changed successfully." });
    }
}
