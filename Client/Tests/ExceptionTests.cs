using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

namespace Client.Implementation.Tests;

public class ExceptionTests
{
    #region FeatureSwitcherException Tests

    [Fact]
    public void FeatureSwitcherException_SetsMessageCorrectly()
    {
        var exception = new FeatureSwitcherException("Test message");

        Assert.Equal("Test message", exception.Message);
    }

    [Fact]
    public void FeatureSwitcherException_SetsCodeToZero_ByDefault()
    {
        var exception = new FeatureSwitcherException("Test message");

        Assert.Equal(0, exception.Code);
    }

    [Fact]
    public void FeatureSwitcherException_SetsCodeCorrectly()
    {
        var exception = new FeatureSwitcherException("Test message", 42);

        Assert.Equal(42, exception.Code);
    }

    [Fact]
    public void FeatureSwitcherException_InheritsFromException()
    {
        var exception = new FeatureSwitcherException("Test message");

        Assert.IsAssignableFrom<Exception>(exception);
    }

    #endregion

    #region FeatureNotRegisteredException Tests

    [Fact]
    public void FeatureNotRegisteredException_SetsMessageCorrectly()
    {
        var exception = new FeatureNotRegisteredException("Feature not found");

        Assert.Equal("Feature not found", exception.Message);
    }

    [Fact]
    public void FeatureNotRegisteredException_SetsCodeTo1()
    {
        var exception = new FeatureNotRegisteredException("Feature not found");

        Assert.Equal(1, exception.Code);
    }

    [Fact]
    public void FeatureNotRegisteredException_InheritsFromFeatureSwitcherException()
    {
        var exception = new FeatureNotRegisteredException("Feature not found");

        Assert.IsAssignableFrom<FeatureSwitcherException>(exception);
    }

    #endregion

    #region NodeUnreachableException Tests

    [Fact]
    public void NodeUnreachableException_SetsMessageCorrectly()
    {
        var exception = new NodeUnreachableException("Node is down");

        Assert.Equal("Node is down", exception.Message);
    }

    [Fact]
    public void NodeUnreachableException_SetsCodeToZero_ByDefault()
    {
        var exception = new NodeUnreachableException("Node is down");

        Assert.Equal(0, exception.Code);
    }

    [Fact]
    public void NodeUnreachableException_SetsCodeCorrectly()
    {
        var exception = new NodeUnreachableException("Node is down", 500);

        Assert.Equal(500, exception.Code);
    }

    [Fact]
    public void NodeUnreachableException_InheritsFromFeatureSwitcherException()
    {
        var exception = new NodeUnreachableException("Node is down");

        Assert.IsAssignableFrom<FeatureSwitcherException>(exception);
    }

    #endregion

    #region FeatureNameCollisionException Tests

    [Fact]
    public void FeatureNameCollisionException_SetsMessageCorrectly()
    {
        var exception = new FeatureNameCollisionException("Duplicate name");

        Assert.Equal("Duplicate name", exception.Message);
    }

    [Fact]
    public void FeatureNameCollisionException_SetsCodeToZero_ByDefault()
    {
        var exception = new FeatureNameCollisionException("Duplicate name");

        Assert.Equal(0, exception.Code);
    }

    [Fact]
    public void FeatureNameCollisionException_SetsCodeCorrectly()
    {
        var exception = new FeatureNameCollisionException("Duplicate name", 100);

        Assert.Equal(100, exception.Code);
    }

    [Fact]
    public void FeatureNameCollisionException_InheritsFromFeatureSwitcherException()
    {
        var exception = new FeatureNameCollisionException("Duplicate name");

        Assert.IsAssignableFrom<FeatureSwitcherException>(exception);
    }

    #endregion

    #region RegistrationException Tests

    [Fact]
    public void RegistrationException_SetsMessageCorrectly()
    {
        var exception = new RegistrationException("Registration failed");

        Assert.Equal("Registration failed", exception.Message);
    }

    [Fact]
    public void RegistrationException_SetsCodeToZero_ByDefault()
    {
        var exception = new RegistrationException("Registration failed");

        Assert.Equal(0, exception.Code);
    }

    [Fact]
    public void RegistrationException_SetsCodeCorrectly()
    {
        var exception = new RegistrationException("Registration failed", 400);

        Assert.Equal(400, exception.Code);
    }

    [Fact]
    public void RegistrationException_InheritsFromFeatureSwitcherException()
    {
        var exception = new RegistrationException("Registration failed");

        Assert.IsAssignableFrom<FeatureSwitcherException>(exception);
    }

    #endregion

    #region EnvironmentMismatchException Tests

    [Fact]
    public void EnvironmentMismatchException_SetsMessageCorrectly()
    {
        var exception = new EnvironmentMismatchException("Environment mismatch");

        Assert.Equal("Environment mismatch", exception.Message);
    }

    [Fact]
    public void EnvironmentMismatchException_SetsCodeTo2()
    {
        var exception = new EnvironmentMismatchException("Environment mismatch");

        Assert.Equal(2, exception.Code);
    }

    [Fact]
    public void EnvironmentMismatchException_InheritsFromFeatureSwitcherException()
    {
        var exception = new EnvironmentMismatchException("Environment mismatch");

        Assert.IsAssignableFrom<FeatureSwitcherException>(exception);
    }

    #endregion
}
