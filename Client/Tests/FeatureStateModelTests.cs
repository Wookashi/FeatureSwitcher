using Wookashi.FeatureSwitcher.Client.Implementation;

namespace Client.Implementation.Tests;

public class FeatureStateModelTests
{
    [Fact]
    public void Constructor_SetsNameCorrectly()
    {
        var model = new FeatureStateModel("TestFeature");

        Assert.Equal("TestFeature", model.Name);
    }

    [Fact]
    public void Constructor_SetsInitialStateToFalse_ByDefault()
    {
        var model = new FeatureStateModel("TestFeature");

        Assert.False(model.InitialState);
    }

    [Fact]
    public void Constructor_SetsCurrentLocalStateToInitialState_WhenFalse()
    {
        var model = new FeatureStateModel("TestFeature", initialState: false);

        Assert.False(model.CurrentLocalState);
    }

    [Fact]
    public void Constructor_SetsCurrentLocalStateToInitialState_WhenTrue()
    {
        var model = new FeatureStateModel("TestFeature", initialState: true);

        Assert.True(model.CurrentLocalState);
    }

    [Fact]
    public void Constructor_SetsInitialStateCorrectly_WhenTrue()
    {
        var model = new FeatureStateModel("TestFeature", initialState: true);

        Assert.True(model.InitialState);
    }

    [Fact]
    public void Constructor_SetsInitialStateCorrectly_WhenFalse()
    {
        var model = new FeatureStateModel("TestFeature", initialState: false);

        Assert.False(model.InitialState);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FeatureStateModel(null!));
    }

    [Fact]
    public void CurrentLocalState_CanBeModified()
    {
        var model = new FeatureStateModel("TestFeature", initialState: false);

        model.CurrentLocalState = true;

        Assert.True(model.CurrentLocalState);
        Assert.False(model.InitialState); // Initial state remains unchanged
    }

    [Fact]
    public void CurrentLocalState_CanBeToggledMultipleTimes()
    {
        var model = new FeatureStateModel("TestFeature", initialState: false);

        model.CurrentLocalState = true;
        Assert.True(model.CurrentLocalState);

        model.CurrentLocalState = false;
        Assert.False(model.CurrentLocalState);

        model.CurrentLocalState = true;
        Assert.True(model.CurrentLocalState);
    }

    [Fact]
    public void InitialState_RemainsUnchanged_WhenCurrentLocalStateIsModified()
    {
        var model = new FeatureStateModel("TestFeature", initialState: true);

        model.CurrentLocalState = false;

        Assert.True(model.InitialState);
        Assert.False(model.CurrentLocalState);
    }

    [Fact]
    public void Constructor_AcceptsEmptyString_ForName()
    {
        var model = new FeatureStateModel("");

        Assert.Equal("", model.Name);
    }

    [Fact]
    public void Constructor_AcceptsWhitespaceString_ForName()
    {
        var model = new FeatureStateModel("   ");

        Assert.Equal("   ", model.Name);
    }

    [Fact]
    public void Constructor_AcceptsSpecialCharacters_InName()
    {
        var model = new FeatureStateModel("Feature-With_Special.Characters:123");

        Assert.Equal("Feature-With_Special.Characters:123", model.Name);
    }
}
