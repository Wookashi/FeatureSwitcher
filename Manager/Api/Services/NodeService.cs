using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Dtos;

namespace Wookashi.FeatureSwitcher.Manager.Api.Services;

internal sealed class NodeService
{
    private readonly INodeRepository _nodeRepository;

    public NodeService(INodeRepository nodeRepository)
    {
        _nodeRepository = nodeRepository;
    }

    public void RegisterNode(NodeRegistrationModel nodeRegistrationModel)
    {
        //TODO Implement node registation.
    }
}