

namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

public sealed class ApplicationFeatureNodeEntity
{
    public required ApplicationEntity Application { get; set; }
    public required FeatureEntity Feature { get; set; }
    public required NodeEntity Node { get; set; }
}