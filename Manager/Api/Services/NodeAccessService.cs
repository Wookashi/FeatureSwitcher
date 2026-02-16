using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Manager.Api.Services;

internal sealed class NodeAccessService
{
    private readonly IUserRepository _userRepository;

    public NodeAccessService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public bool CanAccessNode(int userId, string role, int nodeId)
    {
        if (role == "Admin") return true;
        return _userRepository.HasAccessToNode(userId, nodeId);
    }

    public List<int> GetAccessibleNodeIds(int userId, string role)
    {
        if (role == "Admin") return [];
        return _userRepository.GetAccessibleNodeIds(userId);
    }
}
