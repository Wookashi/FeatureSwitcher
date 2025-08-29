using Microsoft.Extensions.DependencyInjection;

namespace Wookashi.FeatureSwitcher.Node.Core;

public class NodeApplication
{
    public NodeApplication(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }
}
