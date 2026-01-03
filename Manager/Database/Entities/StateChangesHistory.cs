namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

public sealed class StateChangesHistory
{
    public long Id { get; set; }
    public long ApplicationId { get; set; }
    public long FlagId { get; set; }
    public long NodeId { get; set; }

    public bool? OldValue { get; set; }
    public bool? NewValue { get; set; }

    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
}