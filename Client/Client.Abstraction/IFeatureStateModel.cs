namespace Wookashi.FeatureSwitcher.Client.Abstraction;

public interface IFeatureStateModel
{
    public string Name { get; }
    public bool InitialState { get; }
    public bool CurrentLocalState { get; set; }
}