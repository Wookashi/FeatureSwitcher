using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Api.Extensions;
using Wookashi.FeatureSwitcher.Manager.Api.Models;
using Wookashi.FeatureSwitcher.Manager.Api.Services;

namespace Wookashi.FeatureSwitcher.Manager.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
internal class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLog;
    private readonly NodeService _nodeService;

    public UsersController(IUserRepository userRepository, IAuditLogRepository auditLog, NodeService nodeService)
    {
        _userRepository = userRepository;
        _auditLog = auditLog;
        _nodeService = nodeService;
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
        var nodeNames = GetNodeNames();
        var summary = $"system admin: {dto.IsSystemAdmin}; node access: {DescribeNodeAccess(dto.NodeAccess, nodeNames)}";
        _auditLog.AddEntry(adminUsername, "CreateUser", $"Created user '{dto.Username}' ({summary})");

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
        var changes = new List<string>();
        if (request.IsSystemAdmin.HasValue && request.IsSystemAdmin.Value != existing.IsSystemAdmin)
            changes.Add($"system admin: {existing.IsSystemAdmin} -> {request.IsSystemAdmin.Value}");
        if (nodeAccess is not null)
        {
            var nodeAccessDiff = DescribeNodeAccessDiff(existing.NodeAccess, dto.NodeAccess, GetNodeNames());
            if (nodeAccessDiff is not null)
                changes.Add($"node access: {nodeAccessDiff}");
        }

        var summary = changes.Count == 0 ? "no changes" : string.Join("; ", changes);
        _auditLog.AddEntry(adminUsername, "UpdateUser", $"Updated user '{dto.Username}': {summary}");

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

    private Dictionary<int, string> GetNodeNames()
        => _nodeService.GetAllNodes().ToDictionary(n => n.Id, n => n.Name);

    private static string DescribeNodeAccess(List<NodeAccessDto> nodeAccess, Dictionary<int, string> nodeNames)
    {
        if (nodeAccess.Count == 0) return "none";
        return string.Join(", ", nodeAccess.Select(a => $"{NodeName(a.NodeId, nodeNames)}={a.Role}"));
    }

    private static string? DescribeNodeAccessDiff(List<NodeAccessDto> before, List<NodeAccessDto> after, Dictionary<int, string> nodeNames)
    {
        var beforeRoles = before.ToDictionary(a => a.NodeId, a => a.Role);
        var afterRoles = after.ToDictionary(a => a.NodeId, a => a.Role);

        var changes = new List<string>();
        foreach (var nodeId in beforeRoles.Keys.Union(afterRoles.Keys).OrderBy(id => id))
        {
            beforeRoles.TryGetValue(nodeId, out var oldRole);
            afterRoles.TryGetValue(nodeId, out var newRole);
            if (oldRole == newRole) continue;

            var name = NodeName(nodeId, nodeNames);
            if (oldRole is null) changes.Add($"{name}: granted {newRole}");
            else if (newRole is null) changes.Add($"{name}: access removed (was {oldRole})");
            else changes.Add($"{name}: {oldRole} -> {newRole}");
        }

        return changes.Count == 0 ? null : string.Join(", ", changes);
    }

    private static string NodeName(int nodeId, Dictionary<int, string> nodeNames)
        => nodeNames.TryGetValue(nodeId, out var name) ? name : $"#{nodeId}";
}
