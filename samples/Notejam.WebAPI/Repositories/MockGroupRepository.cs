using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Ports;

namespace Looplex.Samples.WebAPI.Repositories;

/// <summary>
/// Mock repository for Group entities - returns sample data for demonstration
/// </summary>
public class MockGroupRepository : IResourceRepository<Group>
{
    private readonly List<Group> _groups = new()
    {
        new Group
        {
            Id = "33333333-3333-3333-3333-333333333333",
            ExternalId = "group1",
            DisplayName = "Engineering Team",
            Meta = new ResourceMeta
            {
                ResourceType = "Group",
                Created = DateTime.UtcNow.AddDays(-60),
                LastModified = DateTime.UtcNow.AddDays(-5)
            }
        },
        new Group
        {
            Id = "44444444-4444-4444-4444-444444444444",
            ExternalId = "group2",
            DisplayName = "Product Management",
            Meta = new ResourceMeta
            {
                ResourceType = "Group",
                Created = DateTime.UtcNow.AddDays(-45),
                LastModified = DateTime.UtcNow.AddDays(-3)
            }
        },
        new Group
        {
            Id = "55555555-5555-5555-5555-555555555555",
            ExternalId = "group3",
            DisplayName = "Administrators",
            Meta = new ResourceMeta
            {
                ResourceType = "Group",
                Created = DateTime.UtcNow.AddDays(-90),
                LastModified = DateTime.UtcNow.AddDays(-10)
            }
        }
    };

    public Task<Group?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var group = _groups.FirstOrDefault(g => g.Id == id);
        return Task.FromResult(group);
    }

    public Task<Group> CreateAsync(Group resource, CancellationToken cancellationToken = default)
    {
        resource.Id = Guid.NewGuid().ToString();
        resource.Meta = new ResourceMeta
        {
            ResourceType = "Group",
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
        _groups.Add(resource);
        return Task.FromResult(resource);
    }

    public Task<Group> UpdateAsync(string id, Group resource, CancellationToken cancellationToken = default)
    {
        var existingGroup = _groups.FirstOrDefault(g => g.Id == id);
        if (existingGroup != null)
        {
            resource.Id = id;
            resource.Meta = new ResourceMeta
            {
                ResourceType = "Group",
                Created = existingGroup.Meta?.Created ?? DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            var index = _groups.IndexOf(existingGroup);
            _groups[index] = resource;
        }
        return Task.FromResult(resource);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var group = _groups.FirstOrDefault(g => g.Id == id);
        if (group != null)
        {
            _groups.Remove(group);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<(IList<Group> Resources, int TotalCount)> QueryAsync(int startIndex, int count, string? filter, CancellationToken cancellationToken = default)
    {
        return QueryAsync(startIndex, count, filter, null, null, null, null, cancellationToken);
    }

    public Task<(IList<Group> Resources, int TotalCount)> QueryAsync(int startIndex, int count, string? filter, string? sortBy, string? sortOrder, string? attributes, string? excludedAttributes, CancellationToken cancellationToken = default)
    {
        var groups = _groups.AsQueryable();
        
        if (!string.IsNullOrEmpty(filter))
        {
            // Simple filter implementation
            if (filter.Contains("displayName co"))
            {
                var displayNameValue = ExtractFilterValue(filter, "displayName co");
                if (!string.IsNullOrEmpty(displayNameValue))
                    groups = groups.Where(g => g.DisplayName.Contains(displayNameValue, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Apply sorting if provided
        if (!string.IsNullOrEmpty(sortBy))
        {
            var isDescending = string.Equals(sortOrder, "descending", StringComparison.OrdinalIgnoreCase);
            
            switch (sortBy.ToLowerInvariant())
            {
                case "id":
                    groups = isDescending ? groups.OrderByDescending(g => g.Id) : groups.OrderBy(g => g.Id);
                    break;
                case "displayname":
                    groups = isDescending ? groups.OrderByDescending(g => g.DisplayName) : groups.OrderBy(g => g.DisplayName);
                    break;
                case "meta.created":
                    groups = isDescending ? groups.OrderByDescending(g => g.Meta != null ? g.Meta.Created : DateTime.MinValue) : groups.OrderBy(g => g.Meta != null ? g.Meta.Created : DateTime.MinValue);
                    break;
                case "meta.lastmodified":
                    groups = isDescending ? groups.OrderByDescending(g => g.Meta != null ? g.Meta.LastModified : DateTime.MinValue) : groups.OrderBy(g => g.Meta != null ? g.Meta.LastModified : DateTime.MinValue);
                    break;
                default:
                    // Default sorting by ID
                    groups = groups.OrderBy(g => g.Id);
                    break;
            }
        }
        else
        {
            // Default sorting by ID
            groups = groups.OrderBy(g => g.Id);
        }

        var totalCount = groups.Count();
        var result = groups.Skip(startIndex - 1).Take(count).ToList();
        return Task.FromResult((result as IList<Group>, totalCount));
    }

    private string? ExtractFilterValue(string filter, string operatorName)
    {
        var pattern = $"{operatorName} \"([^\"]+)\"";
        var match = System.Text.RegularExpressions.Regex.Match(filter, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }
}
