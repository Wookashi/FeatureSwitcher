

using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

public interface INodeRepository
{
    public void CreateOrUpdateNode(string name, Uri address);
    public List<NodeDto> GetAllNodes();
    public NodeDto GetNodeById(int nodeId);

}