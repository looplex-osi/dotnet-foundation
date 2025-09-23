using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Modules;
using Looplex.Foundation.Serialization;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application;

namespace Looplex.Samples.WebApp.Services;

/// <summary>
/// SCIMv2 Note service - independent of MediatR
/// Direct implementation for SCIMv2 endpoints
/// </summary>
public class SCIMv2NoteService : IResourceService<Note>
{
    private readonly INoteRepository _noteRepository;

    public SCIMv2NoteService(INoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    public string CollectionName => "notes";

    /// <summary>
    /// Create a new note from JSON
    /// Generic method for middleware compatibility
    /// </summary>
    /// <param name="json">JSON representation of the note</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of the created note</returns>
    public async Task<Guid> CreateAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"SCIMv2NoteService.CreateAsync - JSON received: {json}");
            
            // Deserialize JSON to Note using Foundation's SCIMv2 serializer
            var note = ActorJsonSerializer.DeserializeResource<Note>(json);
            System.Diagnostics.Debug.WriteLine($"SCIMv2NoteService.CreateAsync - Note deserialized: {note?.Name}");
            
            // Call the repository directly to avoid recursion
            var result = await _noteRepository.CreateNoteAsync(note, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"SCIMv2NoteService.CreateAsync - Repository result: {result}");
            
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SCIMv2NoteService.CreateAsync - ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"SCIMv2NoteService.CreateAsync - StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Replace a note from JSON
    /// Generic method for middleware compatibility
    /// </summary>
    /// <param name="id">Note ID</param>
    /// <param name="json">JSON representation of the note</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    public async Task<bool> ReplaceAsync(string id, string json, CancellationToken cancellationToken = default)
    {
        // Deserialize JSON to Note using Foundation's SCIMv2 serializer
        var note = ActorJsonSerializer.DeserializeResource<Note>(json);
        
        // Call the repository directly to avoid recursion
        var rowsAffected = await _noteRepository.UpdateNoteAsync(Guid.Parse(id), note, cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<(IList<Note> Resources, int TotalCount)> QueryAsync(int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🔍 SCIMv2NoteService.QueryAsync - startIndex: {startIndex}, count: {count}, filter: {filter}");
            
            // Calculate page from startIndex
            var page = (startIndex - 1) / count + 1;
            var pageSize = count;
            
            Console.WriteLine($"🔍 Calculated page: {page}, pageSize: {pageSize}");
            
            var result = await _noteRepository.GetNotesAsync(filter, page, pageSize, cancellationToken);
            
            Console.WriteLine($"🔍 Repository returned {result.Notes.Count} notes, TotalCount: {result.TotalCount}");
            
            // Log successful retrieval
            Console.WriteLine($"🔍 Successfully retrieved {result.Notes.Count} notes");
            
            return (result.Notes, result.TotalCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in SCIMv2NoteService.QueryAsync: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<Guid> CreateAsync(Note resource, CancellationToken cancellationToken = default)
    {
        return await _noteRepository.CreateNoteAsync(resource, cancellationToken);
    }

    public async Task<Note?> RetrieveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _noteRepository.GetNoteByIdAsync(id, cancellationToken);
    }

    public async Task<bool> ReplaceAsync(Guid id, Note resource, CancellationToken cancellationToken = default)
    {
        var result = await _noteRepository.UpdateNoteAsync(id, resource, cancellationToken);
        return result > 0;
    }

    public async Task<bool> ReplaceAsync(Guid id, IResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is Note note)
        {
            return await ReplaceAsync(id, note, cancellationToken);
        }
        return false;
    }

    public async Task<bool> UpdateAsync(Guid id, Note resource, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        // Simple implementation - just replace
        return await ReplaceAsync(id, resource, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _noteRepository.DeleteNoteAsync(id, cancellationToken);
        return result > 0;
    }
}
