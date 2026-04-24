namespace Wookashi.FeatureSwitcher.Client.Abstraction.Models;

public interface IFeatureStateModel
{
    public string Name { get; }
    public bool InitialState { get; }
    public bool CurrentLocalState { get; set; }
}