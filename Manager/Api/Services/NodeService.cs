using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Manager.Api.Services;

internal sealed class NodeService
{
    private readonly INodeRepository _nodeRepository;
    private readonly HttpClient _httpClient;

    public NodeService(INodeRepository nodeRepository, IHttpClientFactory httpClientFactory)
    {
        _nodeRepository = nodeRepository;
        _httpClient = httpClientFactory.CreateClient();
    }

    public void CreateOrReplaceNode(NodeRegistrationModel nodeRegistrationModel)
    {
        _nodeRepository
            .CreateOrUpdateNode(nodeRegistrationModel.NodeName, nodeRegistrationModel.NodeAddress);
    }
    
    public List<NodeDto> GetAllNodes()
    {
        return _nodeRepository.GetAllNodes();
    }
    
    public List<ApplicationDto> GetApplications(int NodeId)
    {
        return _nodeRepository.GetAllNodes();
    }
    
    public List<ApplicationDto> GetFeaturesForApplication(int NodeId, int appId)
    {
        return _nodeRepository.GetAllNodes();
    }
}