using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application;

/// <summary>
/// Repository interface for Pad entities
/// </summary>
public interface IPadRepository
{
    /// <summary>
    /// Gets all active pads
    /// </summary>
    Task<List<Pad>> GetPadsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pads with optional filter
    /// </summary>
    Task<List<Pad>> GetPadsAsync(string? filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pads with pagination
    /// </summary>
    Task<List<Pad>> GetPadsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pads with filter and pagination
    /// </summary>
    Task<List<Pad>> GetPadsAsync(string? filter, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pad by ID
    /// </summary>
    Task<Pad?> GetByIdAsync(string id, CancellationToken cancellationToken = default);


    /// <summary>
    /// Creates a new pad
    /// </summary>
    Task<Guid> CreatePadAsync(Pad pad, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing pad
    /// </summary>
    Task<int> UpdatePadAsync(Guid id, Pad pad, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a pad (soft delete)
    /// </summary>
    Task<int> DeletePadAsync(Guid id, CancellationToken cancellationToken = default);
}

