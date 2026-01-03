using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Manager.Database.Repositories;

internal sealed class NodeInMemoryRepository : INodeRepository
{
    public void CreateOrUpdateNode(NodeDto nodeDto)
    {
        throw new NotImplementedException();
    }

    public void UpdateFeatureState()
    {
        throw new NotImplementedException();
    }
}