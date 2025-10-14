using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Ports;

namespace Looplex.Samples.WebAPI.Repositories;

/// <summary>
/// Mock repository for User entities - returns sample data for demonstration
/// </summary>
public class MockUserRepository : IResourceRepository<User>
{
    private readonly List<User> _users = new()
    {
        new User
        {
            Id = "11111111-1111-1111-1111-111111111111",
            ExternalId = "user1",
            UserName = "john.doe",
            DisplayName = "John Doe",
            Active = true,
            Meta = new ResourceMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow.AddDays(-30),
                LastModified = DateTime.UtcNow.AddDays(-1)
            }
        },
        new User
        {
            Id = "22222222-2222-2222-2222-222222222222",
            ExternalId = "user2",
            UserName = "jane.smith",
            DisplayName = "Jane Smith",
            Active = true,
            Meta = new ResourceMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow.AddDays(-15),
                LastModified = DateTime.UtcNow.AddHours(-2)
            }
        }
    };

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<User> CreateAsync(User resource, CancellationToken cancellationToken = default)
    {
        resource.Id = Guid.NewGuid().ToString();
        resource.Meta = new ResourceMeta
        {
            ResourceType = "User",
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
        _users.Add(resource);
        return Task.FromResult(resource);
    }

    public Task<User> UpdateAsync(string id, User resource, CancellationToken cancellationToken = default)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == id);
        if (existingUser != null)
        {
            resource.Id = id;
            resource.Meta = new ResourceMeta
            {
                ResourceType = "User",
                Created = existingUser.Meta?.Created ?? DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            var index = _users.IndexOf(existingUser);
            _users[index] = resource;
        }
        return Task.FromResult(resource);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            _users.Remove(user);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<(IList<User> Resources, int TotalCount)> QueryAsync(int startIndex, int count, string? filter, CancellationToken cancellationToken = default)
    {
        return QueryAsync(startIndex, count, filter, null, null, null, null, cancellationToken);
    }

    public Task<(IList<User> Resources, int TotalCount)> QueryAsync(int startIndex, int count, string? filter, string? sortBy, string? sortOrder, string? attributes, string? excludedAttributes, CancellationToken cancellationToken = default)
    {
        var users = _users.AsQueryable();
        
        if (!string.IsNullOrEmpty(filter))
        {
            // Simple filter implementation
            if (filter.Contains("active eq true"))
                users = users.Where(u => u.Active);
            else if (filter.Contains("active eq false"))
                users = users.Where(u => !u.Active);
        }

        // Apply sorting if provided
        if (!string.IsNullOrEmpty(sortBy))
        {
            var isDescending = string.Equals(sortOrder, "descending", StringComparison.OrdinalIgnoreCase);
            
            switch (sortBy.ToLowerInvariant())
            {
                case "id":
                    users = isDescending ? users.OrderByDescending(u => u.Id) : users.OrderBy(u => u.Id);
                    break;
                case "username":
                    users = isDescending ? users.OrderByDescending(u => u.UserName) : users.OrderBy(u => u.UserName);
                    break;
                case "displayname":
                    users = isDescending ? users.OrderByDescending(u => u.DisplayName) : users.OrderBy(u => u.DisplayName);
                    break;
                case "active":
                    users = isDescending ? users.OrderByDescending(u => u.Active) : users.OrderBy(u => u.Active);
                    break;
                case "meta.created":
                    users = isDescending ? users.OrderByDescending(u => u.Meta != null ? u.Meta.Created : DateTime.MinValue) : users.OrderBy(u => u.Meta != null ? u.Meta.Created : DateTime.MinValue);
                    break;
                case "meta.lastmodified":
                    users = isDescending ? users.OrderByDescending(u => u.Meta != null ? u.Meta.LastModified : DateTime.MinValue) : users.OrderBy(u => u.Meta != null ? u.Meta.LastModified : DateTime.MinValue);
                    break;
                default:
                    // Default sorting by ID
                    users = users.OrderBy(u => u.Id);
                    break;
            }
        }
        else
        {
            // Default sorting by ID
            users = users.OrderBy(u => u.Id);
        }

        var totalCount = users.Count();
        var result = users.Skip(startIndex - 1).Take(count).ToList();
        return Task.FromResult((result as IList<User>, totalCount));
    }
}
