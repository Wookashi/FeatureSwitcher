using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation;
using Wookashi.FeatureSwitcher.Client.Implementation.Models;

namespace Client.Tests;

public class FeatureManagerTests
{
    [Fact]
    public void FeatureManager_ThrowErrorWhenFeatureNotExists()
    {
        List<FeatureStateModel> features = new List<FeatureStateModel>();
        var manager = new FeatureManager("TestApp", features);
        
        Assert.Throws<FeatureNotRegisteredException>(
            () => manager.IsFeatureEnabled("fakeTestFlag"));
    }
    
    [Fact]
    public void FeatureManager_CheckFalseFeatureState()
    {
        List<FeatureStateModel> features =
        [
            new FeatureStateModel("TestFlag", false)
        ];
        var manager = new FeatureManager("TestApp", features);

        var result = manager.IsFeatureEnabled("TestFlag");
        
        Assert.False(result);
    }
    
    [Fact]
    public void FeatureManager_CheckTrueFeatureState()
    {
        List<FeatureStateModel> features =
        [
            new FeatureStateModel("TestFlag", true)
        ];
        var manager = new FeatureManager("TestApp", features);

        var result = manager.IsFeatureEnabled("TestFlag");
        
        Assert.True(result);
    }
}