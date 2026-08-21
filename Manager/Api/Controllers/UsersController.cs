using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Api.Extensions;
using Wookashi.FeatureSwitcher.Manager.Api.Models;

namespace Wookashi.FeatureSwitcher.Manager.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
internal class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLog;

    public UsersController(IUserRepository userRepository, IAuditLogRepository auditLog)
    {
        _userRepository = userRepository;
        _auditLog = auditLog;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_userRepository.GetAllUsers().ToList());
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var dto = _userRepository.GetUserById(id);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Username and password are required." });

        if (request.NodeAccess.Any(a => a.Role is not ("Viewer" or "Editor")))
            return BadRequest(new { error = "Node access role must be Viewer or Editor." });

        if (_userRepository.GetUserByUsername(request.Username.Trim()) is not null)
            return Conflict(new { error = "A user with this username already exists." });

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var nodeAccess = request.NodeAccess.Select(a => new NodeAccessDto(a.NodeId, a.Role)).ToList();
        var dto = _userRepository.CreateUser(request.Username.Trim(), hash, request.IsSystemAdmin, nodeAccess);

        var adminUsername = User.GetUserName();
        _auditLog.AddEntry(adminUsername, "CreateUser", $"Created user '{dto.Username}' (system admin: {dto.IsSystemAdmin})");

        return Created($"/api/users/{dto.Id}", dto);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] UpdateUserRequest request)
    {
        var existing = _userRepository.GetUserById(id);
        if (existing is null) return NotFound();

        if (request.NodeAccess?.Any(a => a.Role is not ("Viewer" or "Editor")) == true)
            return BadRequest(new { error = "Node access role must be Viewer or Editor." });

        var nodeAccess = request.NodeAccess?.Select(a => new NodeAccessDto(a.NodeId, a.Role)).ToList();
        var dto = _userRepository.UpdateUser(id, request.IsSystemAdmin, nodeAccess);

        var adminUsername = User.GetUserName();
        _auditLog.AddEntry(adminUsername, "UpdateUser", $"Updated user '{dto.Username}'");

        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var currentUserId = User.GetUserId();
        var adminUsername = User.GetUserName();
        if (currentUserId == id)
            return BadRequest(new { error = "Cannot delete your own account." });

        var existing = _userRepository.GetUserById(id);
        if (existing is null) return NotFound();

        _userRepository.DeleteUser(id);
        _auditLog.AddEntry(adminUsername, "DeleteUser", $"Deleted user '{existing.Username}'");

        return NoContent();
    }
}
