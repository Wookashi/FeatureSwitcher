

using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

public interface INodeRepository
{
    public void CreateOrUpdateNode(string name, Uri address);
    public void DeleteNode(int nodeId);
    public List<NodeDto> GetAllNodes();
    public NodeDto GetNodeById(int nodeId);

}