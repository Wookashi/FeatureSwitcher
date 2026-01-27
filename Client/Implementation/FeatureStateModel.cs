using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Default implementation of a feature state.
/// Use this class to define features that your application will check.
/// </summary>
/// <example>
/// var features = new List&lt;FeatureStateModel&gt;
/// {
///     new("DarkMode", initialState: false),
///     new("NewCheckout", initialState: true),
/// };
/// </example>
public class FeatureStateModel : IFeatureStateModel
{
    /// <summary>
    /// The unique name of this feature.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The initial state of this feature (used when the node is unreachable).
    /// </summary>
    public bool InitialState { get; }

    /// <summary>
    /// The current cached state of this feature.
    /// Updated when the node is queried successfully.
    /// </summary>
    public bool CurrentLocalState { get; set; }

    /// <summary>
    /// Creates a new feature state.
    /// </summary>
    /// <param name="name">Unique name for this feature.</param>
    /// <param name="initialState">Initial state (defaults to false/disabled).</param>
    public FeatureStateModel(string name, bool initialState = false)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        InitialState = initialState;
        CurrentLocalState = initialState;
    }
}
