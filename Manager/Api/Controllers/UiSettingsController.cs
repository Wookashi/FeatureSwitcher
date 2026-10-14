using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Manager.Api.Configuration;

namespace Wookashi.FeatureSwitcher.Manager.Api.Controllers;

/// <summary>
/// Server-side settings the web UI needs to render, so they are configured in one place (the Manager's
/// configuration) instead of being built into the UI.
/// </summary>
[ApiController]
[Route("api/ui-settings")]
[Authorize]
internal class UiSettingsController : ControllerBase
{
    private readonly FeatureUsageSettings _featureUsage;

    public UiSettingsController(IOptions<FeatureUsageSettings> featureUsage)
    {
        _featureUsage = featureUsage.Value;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var unreadWarningAfter = _featureUsage.UnreadWarningAfter;
        return Ok(new
        {
            // null = warning disabled
            unreadWarningAfterDays = unreadWarningAfter > TimeSpan.Zero
                ? (int?)Math.Max(1, (int)Math.Round(unreadWarningAfter.TotalDays))
                : null,
        });
    }
}
