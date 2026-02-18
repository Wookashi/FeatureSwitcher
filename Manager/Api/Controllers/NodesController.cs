using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Api.Extensions;
using Wookashi.FeatureSwitcher.Manager.Api.Services;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Manager.Api.Controllers;

[ApiController]
[Route("api/nodes")]
[Authorize]
internal class NodesController : ControllerBase
{
    private readonly NodeService _nodeService;
    private readonly NodeAccessService _nodeAccessService;
    private readonly IAuditLogRepository _auditLog;

    public NodesController(NodeService nodeService, NodeAccessService nodeAccessService, IAuditLogRepository auditLog)
    {
        _nodeService = nodeService;
        _nodeAccessService = nodeAccessService;
        _auditLog = auditLog;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var allNodes = _nodeService.GetAllNodes();

        var userId = User.GetUserId();
        var role = User.GetUserRole();

        if (role == "Admin")
            return Ok(allNodes);

        var accessibleIds = _nodeAccessService.GetAccessibleNodeIds(userId, role);
        var filtered = allNodes.Where(n => accessibleIds.Contains(n.Id)).ToList();
        return Ok(filtered);
    }

    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult Register([FromBody] NodeRegistrationModel nodeRegistrationModel)
    {
        _nodeService.CreateOrReplaceNode(nodeRegistrationModel);
        return Created();
    }

    [HttpGet("{nodeId:int}/applications")]
    public async Task<IActionResult> GetApplications(int nodeId)
    {
        var userId = User.GetUserId();
        var role = User.GetUserRole();

        if (!_nodeAccessService.CanAccessNode(userId, role, nodeId))
            return Forbid();

        var apps = await _nodeService.GetApplicationsAsync(nodeId);
        return Ok(apps);
    }

    [HttpGet("{nodeId:int}/applications/{appName}/features")]
    public async Task<IActionResult> GetFeatures(int nodeId, string appName)
    {
        var userId = User.GetUserId();
        var role = User.GetUserRole();
        if (!_nodeAccessService.CanAccessNode(userId, role, nodeId))
            return Forbid();

        var features = await _nodeService.GetFeaturesForApplicationAsync(nodeId, appName);
        return Ok(features);
    }

    [HttpPut("{nodeId:int}/applications/{appName}/features/{featureName}")]
    [Authorize(Policy = "EditorOrAdmin")]
    public async Task<IActionResult> SetFeatureState(int nodeId, string appName, string featureName,
        [FromBody] FeatureStateModel featureState)
    {
        var userId = User.GetUserId();
        var username = User.GetUserName();
        var role = User.GetUserRole();

        if (!_nodeAccessService.CanAccessNode(userId, role, nodeId))
            return Forbid();

        featureState.ChangedBy = username;

        var response = await _nodeService.SetFeatureStateAsync(nodeId, appName, featureName, featureState);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            _auditLog.AddEntry(username, "ToggleFeature",
                $"{featureName} in {appName} on node {nodeId} set to {featureState.State}");
        }

        return response.StatusCode switch
        {
            HttpStatusCode.OK => Ok(),
            HttpStatusCode.BadRequest => BadRequest(),
            _ => StatusCode(500)
        };
    }
}
