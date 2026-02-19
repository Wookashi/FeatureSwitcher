using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Manager.Api.Controllers;

[ApiController]
[Route("api/audit-log")]
[Authorize(Policy = "AdminOnly")]
internal class AuditLogController : ControllerBase
{
    private readonly IAuditLogRepository _auditLog;

    public AuditLogController(IAuditLogRepository auditLog)
    {
        _auditLog = auditLog;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] int count = 50, [FromQuery] int offset = 0)
    {
        if (count <= 0) count = 50;
        if (offset < 0) offset = 0;
        return Ok(_auditLog.GetRecentEntries(count, offset));
    }
}
