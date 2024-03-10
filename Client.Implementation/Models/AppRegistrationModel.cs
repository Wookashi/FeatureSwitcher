namespace Wookashi.FeatureSwitcher.Client.Implementation.Models;

public record AppRegistrationModel(string? ApplicationName, string? Environment, List<FeatureStateModel> Features);