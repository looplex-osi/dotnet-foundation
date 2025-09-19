using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.SCIMv2.Modules;
using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.SCIMv2.Examples;

/// <summary>
/// Example implementation of how to use the new SCIMv2Service
/// This demonstrates the pattern for applications to follow
/// </summary>
public class SCIMv2ServiceExample
{
    private readonly ISCIMv2 _scimService;

    public SCIMv2ServiceExample(ISCIMv2 scimService)
    {
        _scimService = scimService ?? throw new ArgumentNullException(nameof(scimService));
    }

    /// <summary>
    /// Example of how to register resource services
    /// </summary>
    public void RegisterServices()
    {
        // Register Users collection
        _scimService.Register(new UserResourceService(), "Users");
        
        // Register Groups collection
        _scimService.Register(new GroupResourceService(), "Groups");
    }

    /// <summary>
    /// Example of how to query resources
    /// </summary>
    public async Task<SCIMv2Response> QueryUsersExample()
    {
        return await _scimService.QueryAsync("Users", 1, 10, null, null, null);
    }

    /// <summary>
    /// Example of how to create a resource
    /// </summary>
    public async Task<SCIMv2Response> CreateUserExample()
    {
        var user = new User
        {
            UserName = "john.doe",
            DisplayName = "John Doe",
            Active = true,
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" }
        };

        return await _scimService.CreateAsync("Users", user);
    }
}

/// <summary>
/// Example User resource service implementation
/// </summary>
public class UserResourceService : IResourceService<User>
{
    private readonly List<User> _users = new();
    
    public string CollectionName => "Users";

    public async Task<(IList<User> Resources, int TotalCount)> QueryAsync(int startIndex, int count,
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default)
    {
        var allUsers = await GetAllUsersAsync(cancellationToken);
        
        // Apply filtering, sorting, and pagination
        var filteredUsers = allUsers;
        if (!string.IsNullOrEmpty(filter))
        {
            filteredUsers = allUsers.Where(u => u.UserName?.ToLower().Contains(filter.ToLower()) == true).ToList();
        }
        
        var sortedUsers = filteredUsers;
        if (!string.IsNullOrEmpty(sortBy))
        {
            var isDescending = string.Equals(sortOrder, "descending", StringComparison.OrdinalIgnoreCase);
            sortedUsers = isDescending 
                ? filteredUsers.OrderByDescending(u => u.UserName).ToList()
                : filteredUsers.OrderBy(u => u.UserName).ToList();
        }
        
        var paginatedUsers = sortedUsers.Skip(startIndex - 1).Take(count).ToList();
        
        return (paginatedUsers, sortedUsers.Count);
    }

    public async Task<Guid> CreateAsync(User resource, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        resource.Id = id.ToString();
        
        resource.Meta = new ResourceMeta
        {
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Version = "1",
            Location = $"/Users/{resource.Id}"
        };
        
        _users.Add(resource);
        return id;
    }

    public async Task<User?> RetrieveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(u => u.Id == id.ToString());
        return user;
    }

    public async Task<bool> ReplaceAsync(Guid id, User resource, CancellationToken cancellationToken = default)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == id.ToString());
        if (existingUser == null)
        {
            return false;
        }
        
        resource.Id = id.ToString();
        resource.Meta = existingUser.Meta;
        resource.Meta.LastModified = DateTime.UtcNow;
        resource.Meta.Version = (int.Parse(resource.Meta.Version) + 1).ToString();
        
        var index = _users.FindIndex(u => u.Id == id.ToString());
        _users[index] = resource;
        
        return true;
    }

    public async Task<bool> UpdateAsync(Guid id, User resource, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(u => u.Id == id.ToString());
        if (user == null)
        {
            return false;
        }
        
        // Apply patches (simplified implementation)
        foreach (var patch in patches)
        {
            if (patch.Op == "replace" && patch.Path == "displayName")
            {
                user.DisplayName = patch.Value?.ToString();
            }
        }
        
        user.Meta.LastModified = DateTime.UtcNow;
        user.Meta.Version = (int.Parse(user.Meta.Version) + 1).ToString();
        
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(u => u.Id == id.ToString());
        if (user == null)
        {
            return false;
        }
        
        _users.RemoveAll(u => u.Id == id.ToString());
        return true;
    }

    private async Task<IList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return _users;
    }
}

/// <summary>
/// Example Group resource service implementation
/// </summary>
public class GroupResourceService : IResourceService<Group>
{
    private readonly List<Group> _groups = new();
    
    public string CollectionName => "Groups";

    public async Task<(IList<Group> Resources, int TotalCount)> QueryAsync(int startIndex, int count,
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default)
    {
        var allGroups = await GetAllGroupsAsync(cancellationToken);
        
        // Apply filtering, sorting, and pagination
        var filteredGroups = allGroups;
        if (!string.IsNullOrEmpty(filter))
        {
            filteredGroups = allGroups.Where(g => g.DisplayName?.ToLower().Contains(filter.ToLower()) == true).ToList();
        }
        
        var sortedGroups = filteredGroups;
        if (!string.IsNullOrEmpty(sortBy))
        {
            var isDescending = string.Equals(sortOrder, "descending", StringComparison.OrdinalIgnoreCase);
            sortedGroups = isDescending 
                ? filteredGroups.OrderByDescending(g => g.DisplayName).ToList()
                : filteredGroups.OrderBy(g => g.DisplayName).ToList();
        }
        
        var paginatedGroups = sortedGroups.Skip(startIndex - 1).Take(count).ToList();
        
        return (paginatedGroups, sortedGroups.Count);
    }

    public async Task<Guid> CreateAsync(Group resource, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        resource.Id = id.ToString();
        
        resource.Meta = new ResourceMeta
        {
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Version = "1",
            Location = $"/Groups/{resource.Id}"
        };
        
        _groups.Add(resource);
        return id;
    }

    public async Task<Group?> RetrieveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = _groups.FirstOrDefault(g => g.Id == id.ToString());
        return group;
    }

    public async Task<bool> ReplaceAsync(Guid id, Group resource, CancellationToken cancellationToken = default)
    {
        var existingGroup = _groups.FirstOrDefault(g => g.Id == id.ToString());
        if (existingGroup == null)
        {
            return false;
        }
        
        resource.Id = id.ToString();
        resource.Meta = existingGroup.Meta;
        resource.Meta.LastModified = DateTime.UtcNow;
        resource.Meta.Version = (int.Parse(resource.Meta.Version) + 1).ToString();
        
        var index = _groups.FindIndex(g => g.Id == id.ToString());
        _groups[index] = resource;
        
        return true;
    }

    public async Task<bool> UpdateAsync(Guid id, Group resource, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        var group = _groups.FirstOrDefault(g => g.Id == id.ToString());
        if (group == null)
        {
            return false;
        }
        
        // Apply patches (simplified implementation)
        foreach (var patch in patches)
        {
            if (patch.Op == "replace" && patch.Path == "displayName")
            {
                group.DisplayName = patch.Value?.ToString();
            }
        }
        
        group.Meta.LastModified = DateTime.UtcNow;
        group.Meta.Version = (int.Parse(group.Meta.Version) + 1).ToString();
        
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = _groups.FirstOrDefault(g => g.Id == id.ToString());
        if (group == null)
        {
            return false;
        }
        
        _groups.RemoveAll(g => g.Id == id.ToString());
        return true;
    }

    private async Task<IList<Group>> GetAllGroupsAsync(CancellationToken cancellationToken = default)
    {
        return _groups;
    }
}
