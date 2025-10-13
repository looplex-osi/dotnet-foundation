using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application.Abstraction;
using Looplex.Samples.Infra.Repositories.Base;
using Looplex.Samples.Infra.Repositories.Mappings;
using Looplex.Samples.Infra.Repositories.Mappers;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.Repositories;

/// <summary>
/// Repository implementation for Pad entities using stored procedures.
/// 
/// This repository implements the Case-Management pattern with stored procedures:
/// - USP_pads_cquery - Collection query (paginação)
/// - USP_pads_pquery - Paginated query (com contagem)
/// - USP_pads_retrieve - Retrieve single pad
/// - USP_pads_create - Create new pad
/// - USP_pads_update - Update existing pad
/// 
/// Maintains full compatibility with IResourceRepository<Pad> interface from Looplex.Foundation.
/// </summary>
public class PadRepositoryStoredProcedure : BaseStoredProcedureRepository<Pad>, IPadRepositories
{
    public PadRepositoryStoredProcedure(
        IDbConnections connections,
        ILogger<PadRepositoryStoredProcedure> logger,
        IStoredProcedureExecutor executor,
        PadEntityMapping mapping,
        PadDataMapper dataMapper)
        : base(connections, logger, executor, mapping, dataMapper)
    {
    }

    protected override string GetResourceName(Pad resource) => resource.Name ?? "Unknown";
    protected override void SetResourceId(Pad resource, string id) => resource.Id = id;

    #region IPadRepositories Implementation

    public async Task<List<Pad>> GetPadsAsync(CancellationToken cancellationToken = default)
    {
        var (pads, _) = await QueryAsync(1, int.MaxValue, null, cancellationToken);
        return pads.ToList();
    }

    public async Task<List<Pad>> GetPadsAsync(string? filter, CancellationToken cancellationToken = default)
    {
        var (pads, _) = await QueryAsync(1, int.MaxValue, filter, cancellationToken);
        return pads.ToList();
    }

    public async Task<List<Pad>> GetPadsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        var startIndex = (page - 1) * pageSize + 1;
        var (pads, _) = await QueryAsync(startIndex, pageSize, null, cancellationToken);
        return pads.ToList();
    }

    public async Task<List<Pad>> GetPadsAsync(string? filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        var startIndex = (page - 1) * pageSize + 1;
        var (pads, _) = await QueryAsync(startIndex, pageSize, filter, cancellationToken);
        return pads.ToList();
    }

    public async Task<Guid> CreatePadAsync(Pad pad, CancellationToken cancellationToken = default)
    {
        var result = await CreateAsync(pad, cancellationToken);
        if (!Guid.TryParse(result.Id, out var guid))
            throw new InvalidOperationException($"Invalid ID returned from CreateAsync: {result.Id}");
        return guid;
    }

    public async Task<int> UpdatePadAsync(Guid id, Pad pad, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(id.ToString(), pad, cancellationToken);
        return 1;
    }

    public async Task<int> DeletePadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await DeleteAsync(id.ToString(), cancellationToken);
        return result ? 1 : 0;
    }

    #endregion
}
