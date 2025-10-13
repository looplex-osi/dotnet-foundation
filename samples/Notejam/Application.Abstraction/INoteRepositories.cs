using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Abstraction;

/// <summary>
/// Repository interface for Note entity operations
/// </summary>
public interface INoteRepositories
{
    /// <summary>
    /// Get notes with optional filtering and pagination
    /// </summary>
    Task<(IList<Note> Notes, int TotalCount)> GetNotesAsync(
        string? filter = null, 
        int page = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single note by ID
    /// </summary>
    Task<Note?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new note
    /// </summary>
    Task<Guid> CreateNoteAsync(Note note, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing note
    /// </summary>
    Task<int> UpdateNoteAsync(Guid id, Note note, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a note (set active = false)
    /// </summary>
    Task<int> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default);
}
