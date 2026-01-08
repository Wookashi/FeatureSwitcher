using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Manager.Api.Services;

internal sealed class NodeService
{
    private readonly INodeRepository _nodeRepository;

    public NodeService(INodeRepository nodeRepository)
    {
        _nodeRepository = nodeRepository;
    }

    public void CreateOrReplaceNode(NodeRegistrationModel nodeRegistrationModel)
    {
        _nodeRepository
            .CreateOrUpdateNode(new NodeDto(nodeRegistrationModel.NodeName, nodeRegistrationModel.NodeAddress));
    }
    
    public List<NodeDto> GetAllNodes()
    {
        return _nodeRepository.GetAllNodes();
    }
}