using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application.Abstraction;
using Looplex.Samples.Infra.Repositories.Base;
using Looplex.Samples.Infra.Repositories.Mappings;
using Looplex.Samples.Infra.Repositories.Mappers;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.Repositories;

/// <summary>
/// Repository implementation for Note entities using stored procedures.
/// 
/// This repository implements the Case-Management pattern with stored procedures:
/// - USP_notes_cquery - Collection query (paginação)
/// - USP_notes_pquery - Paginated query (com contagem)
/// - USP_notes_retrieve - Retrieve single note
/// - USP_notes_create - Create new note
/// - USP_notes_update - Update existing note
/// 
/// Maintains full compatibility with IResourceRepository<Note> interface from Looplex.Foundation.
/// Supports hierarchical filtering by pad (padId filter).
/// </summary>
public class NoteRepositoryStoredProcedure : BaseStoredProcedureRepository<Note>, INoteRepositories
{
    public NoteRepositoryStoredProcedure(
        IDbConnections connections,
        ILogger<NoteRepositoryStoredProcedure> logger,
        IStoredProcedureExecutor executor,
        NoteEntityMapping mapping,
        NoteDataMapper dataMapper)
        : base(connections, logger, executor, mapping, dataMapper)
    {
    }

    protected override string GetResourceName(Note resource) => resource.Name ?? "Unknown";
    protected override void SetResourceId(Note resource, string id) => resource.Id = id;

    /// <summary>
    /// Get notes with optional filtering and pagination
    /// </summary>
    public async Task<(IList<Note> Notes, int TotalCount)> GetNotesAsync(
        string? filter = null, 
        int page = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync((page - 1) * pageSize + 1, pageSize, filter, cancellationToken);
        return (result.Resources, result.TotalCount);
    }

    public async Task<Guid> CreateNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        var result = await CreateAsync(note, cancellationToken);
        return Guid.Parse(result.Id);
    }

    public async Task<int> UpdateNoteAsync(Guid id, Note note, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(id.ToString(), note, cancellationToken);
        return 1;
    }

    public async Task<int> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await DeleteAsync(id.ToString(), cancellationToken);
        return result ? 1 : 0;
    }
}

