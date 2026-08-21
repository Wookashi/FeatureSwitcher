using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Enums;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Manager.Api.Services;

internal sealed class NodeAccessService
{
    private readonly IUserRepository _userRepository;

    public NodeAccessService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public bool CanAccessNode(int userId, bool isSystemAdmin, int nodeId)
    {
        if (isSystemAdmin) return true;
        return _userRepository.HasAccessToNode(userId, nodeId);
    }

    public bool CanEditNode(int userId, bool isSystemAdmin, int nodeId)
    {
        if (isSystemAdmin) return true;
        return _userRepository.GetNodeRole(userId, nodeId) == NodeRoleEnum.Editor;
    }

    public List<int> GetAccessibleNodeIds(int userId, bool isSystemAdmin)
    {
        if (isSystemAdmin) return [];
        return _userRepository.GetAccessibleNodeIds(userId);
    }

    public Dictionary<int, string> GetAccessibleNodesWithRoles(int userId, bool isSystemAdmin)
    {
        if (isSystemAdmin) return [];
        return GetAccessibleNodeIds(userId, isSystemAdmin)
            .ToDictionary(nodeId => nodeId, nodeId => _userRepository.GetNodeRole(userId, nodeId)?.ToString() ?? "Viewer");
    }
}
