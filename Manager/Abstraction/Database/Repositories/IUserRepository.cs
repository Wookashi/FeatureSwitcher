using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Enums;

namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

public interface IUserRepository
{
    bool AnyUsersExist();
    UserDto? GetUserByUsername(string username);
    string? GetPasswordHash(string username);
    UserDto? GetUserById(int id);
    List<UserDto> GetAllUsers();
    UserDto CreateUser(string username, string passwordHash, bool isSystemAdmin, List<NodeAccessDto> nodeAccess);
    UserDto UpdateUser(int id, bool? isSystemAdmin, List<NodeAccessDto>? nodeAccess);
    void UpdatePassword(int id, string passwordHash);
    void DeleteUser(int id);
    bool HasAccessToNode(int userId, int nodeId);
    List<int> GetAccessibleNodeIds(int userId);
    NodeRoleEnum? GetNodeRole(int userId, int nodeId);
}
