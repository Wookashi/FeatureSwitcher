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
    private readonly ILogger<NodesController> _logger;

    public NodesController(NodeService nodeService, NodeAccessService nodeAccessService, IAuditLogRepository auditLog, ILogger<NodesController> logger)
    {
        _nodeService = nodeService;
        _nodeAccessService = nodeAccessService;
        _auditLog = auditLog;
        _logger = logger;
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

        try
        {
            var apps = await _nodeService.GetApplicationsAsync(nodeId);
            return Ok(apps);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Node {NodeId} unreachable while fetching applications", nodeId);
            return StatusCode(StatusCodes.Status502BadGateway, "Node unreachable");
        }
        catch (OperationCanceledException ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Request to node {NodeId} timed out while fetching applications", nodeId);
            return StatusCode(StatusCodes.Status504GatewayTimeout, "Node request timed out");
        }
    }

    [HttpGet("{nodeId:int}/applications/{appName}/features")]
    public async Task<IActionResult> GetFeatures(int nodeId, string appName)
    {
        var userId = User.GetUserId();
        var role = User.GetUserRole();
        if (!_nodeAccessService.CanAccessNode(userId, role, nodeId))
            return Forbid();

        try
        {
            var features = await _nodeService.GetFeaturesForApplicationAsync(nodeId, appName);
            return Ok(features);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Node {NodeId} unreachable while fetching features for app {AppName}", nodeId, appName);
            return StatusCode(StatusCodes.Status502BadGateway, "Node unreachable");
        }
        catch (OperationCanceledException ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Request to node {NodeId} timed out while fetching features for app {AppName}", nodeId, appName);
            return StatusCode(StatusCodes.Status504GatewayTimeout, "Node request timed out");
        }
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
        else
        {
            _logger.LogWarning("Setting feature {FeatureName} in app {AppName} on node {NodeId} failed with status {StatusCode}",
                featureName, appName, nodeId, (int)response.StatusCode);
        }

        return response.StatusCode switch
        {
            HttpStatusCode.OK => Ok(),
            HttpStatusCode.BadRequest => BadRequest(),
            HttpStatusCode.BadGateway => StatusCode(StatusCodes.Status502BadGateway, "Node unreachable"),
            HttpStatusCode.GatewayTimeout => StatusCode(StatusCodes.Status504GatewayTimeout, "Node request timed out"),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
