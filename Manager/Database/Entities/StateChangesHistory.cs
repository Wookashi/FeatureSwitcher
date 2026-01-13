namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

public sealed class StateChangesHistory
{
    public long Id { get; init; }
    public long ApplicationId { get; init; }
    public long FlagId { get; init; }
    public long NodeId { get; init; }

    public bool? OldValue { get; init; }
    public bool? NewValue { get; init; }

    public DateTime ChangedAt { get; init; }
    public string? ChangedBy { get; init; }
}