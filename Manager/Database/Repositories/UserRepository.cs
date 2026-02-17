using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Enums;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database.Repositories;

internal class UserRepository : IUserRepository
{
    private readonly INodeDataContext _context;

    public UserRepository(INodeDataContext context)
    {
        _context = context;
    }

    public bool AnyUsersExist()
    {
        return _context.Users.Any();
    }

    public UserDto? GetUserByUsername(string username)
    {
        return _context.Users
            .Include(u => u.NodeAccess)
            .Where(u => u.Username == username)
            .Select(u => ToDto(u))
            .SingleOrDefault();
    }

    public string? GetPasswordHash(string username)
    {
        return _context.Users
            .Where(u => u.Username == username)
            .Select(u => u.PasswordHash)
            .SingleOrDefault();
    }

    public UserDto? GetUserById(int id)
    {
        return _context.Users
            .Include(u => u.NodeAccess)
            .Where(u => u.Id == id)
            .Select(u => ToDto(u))
            .SingleOrDefault();
    }

    public List<UserDto> GetAllUsers()
    {
        return _context.Users
            .Include(u => u.NodeAccess)
            .Select(u => ToDto(u))
            .ToList();
    }

    public UserDto CreateUser(string username, string passwordHash, string role, List<int> nodeIds)
    {
        var parsedRole = Enum.Parse<UserRoleEnum>(role);
        var now = DateTime.UtcNow;

        var user = new UserEntity
        {
            Username = username,
            PasswordHash = passwordHash,
            RoleEnum = parsedRole,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        foreach (var nodeId in nodeIds)
        {
            _context.UserNodeAccess.Add(new UserNodeAccessEntity
            {
                UserId = user.Id,
                NodeId = nodeId,
            });
        }

        _context.SaveChanges();

        return new UserDto(user.Id, user.Username, user.RoleEnum.ToString(), user.CreatedAt, user.UpdatedAt, nodeIds);
    }

    public UserDto UpdateUser(int id, string? role, List<int>? nodeIds)
    {
        var user = _context.Users
            .Include(u => u.NodeAccess)
            .Single(u => u.Id == id);

        if (role is not null)
        {
            user.RoleEnum = Enum.Parse<UserRoleEnum>(role);
        }

        user.UpdatedAt = DateTime.UtcNow;

        if (nodeIds is not null)
        {
            var existingAccess = _context.UserNodeAccess.Where(a => a.UserId == id).ToList();
            foreach (var access in existingAccess)
            {
                _context.UserNodeAccess.Remove(access);
            }

            foreach (var nodeId in nodeIds)
            {
                _context.UserNodeAccess.Add(new UserNodeAccessEntity
                {
                    UserId = id,
                    NodeId = nodeId,
                });
            }
        }

        _context.SaveChanges();

        var updatedNodeIds = _context.UserNodeAccess
            .Where(a => a.UserId == id)
            .Select(a => a.NodeId)
            .ToList();

        return new UserDto(user.Id, user.Username, user.RoleEnum.ToString(), user.CreatedAt, user.UpdatedAt, updatedNodeIds);
    }

    public void UpdatePassword(int id, string passwordHash)
    {
        var user = _context.Users.Single(u => u.Id == id);
        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();
    }

    public void DeleteUser(int id)
    {
        var user = _context.Users.Single(u => u.Id == id);
        _context.Users.Remove(user);
        _context.SaveChanges();
    }

    public bool HasAccessToNode(int userId, int nodeId)
    {
        return _context.UserNodeAccess.Any(a => a.UserId == userId && a.NodeId == nodeId);
    }

    public List<int> GetAccessibleNodeIds(int userId)
    {
        return _context.UserNodeAccess
            .Where(a => a.UserId == userId)
            .Select(a => a.NodeId)
            .ToList();
    }

    private static UserDto ToDto(UserEntity u)
    {
        return new UserDto(
            u.Id,
            u.Username,
            u.RoleEnum.ToString(),
            u.CreatedAt,
            u.UpdatedAt,
            u.NodeAccess.Select(a => a.NodeId).ToList());
    }
}
