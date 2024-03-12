namespace Wookashi.FeatureSwitcher.Node.Models;

public class RegisterFeaturesRequestModel
{
    private string AppName { get; set; }
    public string Environment { get; set; }
    private List<FeatureStateModel> Features { get; set; }
    
}