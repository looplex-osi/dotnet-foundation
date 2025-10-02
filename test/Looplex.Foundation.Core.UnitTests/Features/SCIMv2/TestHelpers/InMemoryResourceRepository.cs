using Looplex.Foundation.Core.SCIMv2.Entities;
using Looplex.Foundation.Core.SCIMv2.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.Core.UnitTests.Features.SCIMv2.TestHelpers
{
    /// <summary>
    /// In-memory implementation of IResourceRepository for testing purposes
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    public class InMemoryResourceRepository<T> : IResourceRepository<T> where T : class, IResource
    {
        private readonly List<T> _resources = new List<T>();
        private readonly object _lock = new object();

        public Task<T> CreateAsync(T resource, CancellationToken cancellationToken = default)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            lock (_lock)
            {
                // Set ID if not provided
                if (string.IsNullOrEmpty(resource.Id))
                {
                    resource.Id = Guid.NewGuid().ToString();
                }

                // Set metadata if not provided
                if (resource.Meta == null)
                {
                    resource.Meta = new ResourceMeta
                    {
                        ResourceType = typeof(T).Name,
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        Location = $"/{typeof(T).Name}s/{resource.Id}",
                        Version = "W/\"1\""
                    };
                }

                _resources.Add(resource);
                return Task.FromResult(resource);
            }
        }

        public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            lock (_lock)
            {
                var resource = _resources.FirstOrDefault(r => r.Id == id);
                return Task.FromResult(resource);
            }
        }

        public Task<T> UpdateAsync(string id, T resource, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            lock (_lock)
            {
                var existingIndex = _resources.FindIndex(r => r.Id == id);
                if (existingIndex >= 0)
                {
                    // Update metadata
                    if (resource.Meta != null)
                    {
                        resource.Meta.LastModified = DateTime.UtcNow;
                    }
                    
                    _resources[existingIndex] = resource;
                    return Task.FromResult(resource);
                }
                throw new InvalidOperationException($"Resource with ID '{id}' not found");
            }
        }

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            lock (_lock)
            {
                var index = _resources.FindIndex(r => r.Id == id);
                if (index >= 0)
                {
                    _resources.RemoveAt(index);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<(IList<T> Resources, int TotalCount)> QueryAsync(
            int startIndex, 
            int count, 
            string? filter = null, 
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var query = _resources.AsQueryable();

                // Apply filter if provided
                if (!string.IsNullOrEmpty(filter))
                {
                    // Simple filter implementation for testing
                    query = query.Where(r => 
                        r.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        (r.Meta != null && r.Meta.ResourceType != null && r.Meta.ResourceType.Contains(filter, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = query.Count();
                var resources = query.Skip(startIndex - 1).Take(count).ToList();

                return Task.FromResult(((IList<T>)resources, totalCount));
            }
        }


        /// <summary>
        /// Helper method to add resources for testing
        /// </summary>
        public void AddResource(T resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            lock (_lock)
            {
                _resources.Add(resource);
            }
        }

        /// <summary>
        /// Helper method to clear all resources for testing
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _resources.Clear();
            }
        }

        /// <summary>
        /// Helper method to get all resources for testing
        /// </summary>
        public IEnumerable<T> GetAllResources()
        {
            lock (_lock)
            {
                return _resources.ToList();
            }
        }
    }
}
