using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Modules;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Samples.Application.Commands;
using Looplex.Samples.Application.Queries;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Newtonsoft.Json.Linq;

namespace Looplex.Samples.Application.Services;

/// <summary>
/// SCIMv2 Resource Service for Notes
/// Implements IResourceService<Note> to integrate with the new SCIMv2 architecture
/// </summary>
public class NoteResourceService : IResourceService<Note>
{
    private readonly IMediator _mediator;

    public NoteResourceService(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public string CollectionName => "Notes";

    /// <summary>
    /// Query notes with filtering, sorting and pagination
    /// </summary>
    public async Task<(IList<Note> Resources, int TotalCount)> QueryAsync(int startIndex, int count, string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken)
    {
        try
        {
            var page = Page(startIndex, count);
            var query = new QueryResource<Note>(page, count, filter ?? string.Empty, sortBy ?? string.Empty, sortOrder ?? string.Empty);
            var (result, totalResults) = await _mediator.Send(query, cancellationToken);

            return (result ?? new List<Note>(), totalResults);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to query notes: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create a new note
    /// </summary>
    public async Task<Guid> CreateAsync(Note resource, CancellationToken cancellationToken)
    {
        try
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            resource.Validate();
            var command = new CreateNoteCommand(resource);
            var result = await _mediator.Send(command, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create note: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieve a note by ID
    /// </summary>
    public async Task<Note?> RetrieveAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new RetrieveResource<Note>(id);
            var result = await _mediator.Send(query, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve note {id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Replace a note completely
    /// </summary>
    public async Task<bool> ReplaceAsync(Guid id, Note resource, CancellationToken cancellationToken)
    {
        try
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            resource.Validate();
            var command = new UpdateNoteCommand(id, resource);
            var rows = await _mediator.Send(command, cancellationToken);
            return rows > 0;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to replace note {id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> ReplaceAsync(Guid id, IResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is Note noteResource)
        {
            return await ReplaceAsync(id, noteResource, cancellationToken);
        }
        throw new ArgumentException($"Resource must be of type {typeof(Note).Name}", nameof(resource));
    }

    /// <summary>
    /// Update a note with patches
    /// </summary>
    public async Task<bool> UpdateAsync(Guid id, Note resource, PatchOperation[] patches, CancellationToken cancellationToken)
    {
        try
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            resource.Validate();
            
            // Convert PatchOperation[] to JArray for compatibility with existing command
            var jArray = new JArray();
            foreach (var patch in patches)
            {
                jArray.Add(new JObject
                {
                    ["op"] = patch.Op,
                    ["path"] = patch.Path,
                    ["value"] = patch.Value != null ? JToken.FromObject(patch.Value) : null
                });
            }
            
            var command = new UpdateNoteCommand(id, resource, jArray);
            var rows = await _mediator.Send(command, cancellationToken);
            return rows > 0;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update note {id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Delete a note
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteNoteCommand(id);
            var rows = await _mediator.Send(command, cancellationToken);
            return rows > 0;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete note {id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Helper method to calculate page number
    /// </summary>
    private static int Page(int startIndex, int count)
    {
        return (int)Math.Ceiling((double)startIndex / count);
    }
}
