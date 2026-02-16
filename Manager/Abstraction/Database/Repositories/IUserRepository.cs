using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

public interface IUserRepository
{
    bool AnyUsersExist();
    UserDto? GetUserByUsername(string username);
    string? GetPasswordHash(string username);
    UserDto? GetUserById(int id);
    List<UserDto> GetAllUsers();
    UserDto CreateUser(string username, string passwordHash, string role, List<int> nodeIds);
    UserDto UpdateUser(int id, string? role, List<int>? nodeIds);
    void UpdatePassword(int id, string passwordHash);
    void DeleteUser(int id);
    bool HasAccessToNode(int userId, int nodeId);
    List<int> GetAccessibleNodeIds(int userId);
}
