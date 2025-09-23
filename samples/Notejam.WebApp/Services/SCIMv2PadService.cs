using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Modules;
using Looplex.Foundation.Serialization;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application;

namespace Looplex.Samples.WebApp.Services;

/// <summary>
/// SCIMv2 Pad service - independent of MediatR
/// Direct implementation for SCIMv2 endpoints
/// </summary>
public class SCIMv2PadService : IResourceService<Pad>
{
    private readonly IPadRepository _padRepository;

    public SCIMv2PadService(IPadRepository padRepository)
    {
        _padRepository = padRepository;
    }

    public string CollectionName => "pads";

    /// <summary>
    /// Create a new pad from JSON
    /// Generic method for middleware compatibility
    /// </summary>
    /// <param name="json">JSON representation of the pad</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of the created pad</returns>
    public async Task<Guid> CreateAsync(string json, CancellationToken cancellationToken = default)
    {
        // Deserialize JSON to Pad using Foundation's SCIMv2 serializer
        var pad = ActorJsonSerializer.DeserializeResource<Pad>(json);
        
        // Call the repository directly to avoid recursion
        return await _padRepository.CreatePadAsync(pad, cancellationToken);
    }

    /// <summary>
    /// Replace a pad from JSON
    /// Generic method for middleware compatibility
    /// </summary>
    /// <param name="id">Pad ID</param>
    /// <param name="json">JSON representation of the pad</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    public async Task<bool> ReplaceAsync(string id, string json, CancellationToken cancellationToken = default)
    {
        // Deserialize JSON to Pad using Foundation's SCIMv2 serializer
        var pad = ActorJsonSerializer.DeserializeResource<Pad>(json);
        
        // Call the repository directly to avoid recursion
        var rowsAffected = await _padRepository.UpdatePadAsync(Guid.Parse(id), pad, cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<(IList<Pad> Resources, int TotalCount)> QueryAsync(int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🔍 SCIMv2PadService.QueryAsync - startIndex: {startIndex}, count: {count}, filter: {filter}");
            
            // Calculate page from startIndex
            var page = (startIndex - 1) / count + 1;
            var pageSize = count;
            
            Console.WriteLine($"🔍 Calculated page: {page}, pageSize: {pageSize}");
            
            var pads = await _padRepository.GetPadsAsync(filter, page, pageSize, cancellationToken);
            Console.WriteLine($"🔍 Repository returned {pads.Count} pads");
            
            // Note: IPadRepository doesn't return total count, so we'll use the list count
            Console.WriteLine($"🔍 Returning {pads.Count} pads with TotalCount: {pads.Count}");
            return (pads, pads.Count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in SCIMv2PadService.QueryAsync: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<Guid> CreateAsync(Pad resource, CancellationToken cancellationToken = default)
    {
        return await _padRepository.CreatePadAsync(resource, cancellationToken);
    }

    public async Task<Pad?> RetrieveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _padRepository.GetPadByIdAsync(id, cancellationToken);
    }

    public async Task<bool> ReplaceAsync(Guid id, Pad resource, CancellationToken cancellationToken = default)
    {
        var result = await _padRepository.UpdatePadAsync(id, resource, cancellationToken);
        return result > 0;
    }

    public async Task<bool> ReplaceAsync(Guid id, IResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is Pad pad)
        {
            return await ReplaceAsync(id, pad, cancellationToken);
        }
        return false;
    }

    public async Task<bool> UpdateAsync(Guid id, Pad resource, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        // Simple implementation - just replace
        return await ReplaceAsync(id, resource, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _padRepository.DeletePadAsync(id, cancellationToken);
        return result > 0;
    }
}
