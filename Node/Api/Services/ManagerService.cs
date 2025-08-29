using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

internal sealed class ManagerService
{
    private readonly ManagerSettings _managerSettings;

    internal ManagerService(IOptions<ManagerSettings> managerSettings)
    {
        _managerSettings = managerSettings.Value;
    }

    internal void RegisterOptionsToManager()
    {
        //TODO Implement register method
    }
    
}