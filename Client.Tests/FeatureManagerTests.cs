namespace Client.Tests;

public class FeatureManagerTests
{
    //TODO How to MOQ HttpClientFactory??
    // [Fact]
    // public void FeatureManager_ThrowErrorWhenFeatureNotExists()
    // {
    //     List<FeatureStateModel> features = new List<FeatureStateModel>();
    //     var manager = new FeatureManager("TestApp", features);
    //     
    //     Assert.ThrowsAsync<FeatureNotRegisteredException>(
    //         () => manager.IsFeatureEnabledAsync("fakeTestFlag"));
    // }
    //
    // [Fact]
    // public void FeatureManager_CheckFalseFeatureState()
    // {
    //     List<FeatureStateModel> features =
    //     [
    //         new FeatureStateModel("TestFlag", false)
    //     ];
    //     var manager = new FeatureManager("TestApp", features);
    //
    //     var result = manager.IsFeatureEnabledAsync("TestFlag");
    //     
    //     Assert.False(result);
    // }
    //
    // [Fact]
    // public void FeatureManager_CheckTrueFeatureState()
    // {
    //     List<FeatureStateModel> features =
    //     [
    //         new FeatureStateModel("TestFlag", true)
    //     ];
    //     var manager = new FeatureManager("TestApp", features);
    //
    //     var result = manager.IsFeatureEnabled("TestFlag");
    //     
    //     Assert.True(result);
    // }
}