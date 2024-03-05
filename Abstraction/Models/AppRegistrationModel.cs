namespace Wookashi.FeatureSwitcher.Abstraction.Models;

public record AppRegistrationModel(string? ApplicationName, string? Environment, List<FeatureStateModel> Features);