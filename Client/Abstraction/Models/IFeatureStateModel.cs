namespace Wookashi.FeatureSwitcher.Client.Abstraction.Models;

/// <summary>
/// A feature flag used by the application. Implement <see cref="IFeatureDetails"/> as well to describe the
/// feature in the Manager; the built-in <c>FeatureStateModel</c> implements both.
/// </summary>
public interface IFeatureStateModel
{
    /// <summary>
    /// Name of the feature, unique within the application. Applications on the same Node that register the
    /// same name share one flag state.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// State used when the feature is created on the Node and while the Node has not been reached yet.
    /// </summary>
    public bool InitialState { get; }

    /// <summary>
    /// Last known state, updated whenever the Node is queried successfully; returned while the Node is
    /// unreachable.
    /// </summary>
    public bool CurrentLocalState { get; set; }
}