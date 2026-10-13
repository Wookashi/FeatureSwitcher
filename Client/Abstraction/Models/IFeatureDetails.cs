namespace Wookashi.FeatureSwitcher.Client.Abstraction.Models;

/// <summary>
/// Optional details of a feature, shown in the Manager when the feature is opened: what it does and where
/// to find more. Implement it together with <see cref="IFeatureStateModel"/>; the built-in
/// <c>FeatureStateModel</c> already does.
/// </summary>
/// <remarks>
/// Details belong to the application that registers the feature: when several applications share a flag,
/// each one describes its own use of it. They are sent to the Node with every registration, so removing
/// them from the code removes them from the Manager.
/// <para>
/// The description is plain text (never HTML) with light formatting: a blank line starts a new paragraph,
/// lines starting with <c>"- "</c> form a bullet list, and <c>http(s)</c> URLs become links. Limits are in
/// <see cref="FeatureDetailsLimits"/>; definitions that exceed them are rejected when the feature manager
/// is created.
/// </para>
/// </remarks>
public interface IFeatureDetails
{
    /// <summary>
    /// What the feature does and what happens when it is switched off; <c>null</c> when not described.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Links to related resources such as tickets, documentation, runbooks or dashboards; empty when none.
    /// </summary>
    IReadOnlyList<FeatureLink> Links { get; }
}
