using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Client.Implementation.Tests.Models;

internal sealed class FeatureStateTestModel(string name, bool initialState, bool currentLocalState) : IFeatureStateModel
{
    public string Name { get; } = name;
    public bool InitialState { get; } = initialState;
    public bool CurrentLocalState { get; set; } = currentLocalState;
}