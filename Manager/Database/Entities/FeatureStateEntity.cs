using System.ComponentModel.DataAnnotations;

namespace Wookashi.FeatureSwitcher.Manager.Database.Entities;

public sealed class FeatureStateEntity
{
    [Key]
    public int Id { get; set; }

    // PK złożony (App, Flag, Node)
    public long ApplicationId { get; set; }
    public long FeatureId { get; set; }
    public long NodeId { get; set; }

    // NULL = „nie ustawiono/nie dotyczy”, TRUE/FALSE = wartość jawna
    public bool? Value { get; set; }


    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; } //TODO Maybe UserID?

    public required ApplicationEntity Application { get; set; }
    public required FeatureEntity Feature { get; set; }
    public required NodeEntity Node { get; set; }
}