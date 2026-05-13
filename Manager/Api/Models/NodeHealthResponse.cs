using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Wookashi.FeatureSwitcher.Manager.Api.Models;

public sealed class NodeHealthResponse
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HealthStatus Status { get; set; } = HealthStatus.Unhealthy;
    public string? Version { get; set; }
}