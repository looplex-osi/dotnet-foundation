using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Modules;
using Looplex.Samples.Application.Commands;
using Looplex.Samples.Application.Queries;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Newtonsoft.Json.Linq;

namespace Looplex.Samples.Application.Services;

/// <summary>
/// SCIMv2 Resource Service for Pads
/// Implements IResourceService<Pad> to integrate with the new SCIMv2 architecture
/// </summary>
public class PadResourceService : IResourceService<Pad>
{
    private readonly IMediator _mediator;

    public PadResourceService(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public string CollectionName => "Pads";

    /// <summary>
    /// Query pads with filtering, sorting and pagination
    /// </summary>
    public async Task<(IList<Pad> Resources, int TotalCount)> QueryAsync(int startIndex, int count, string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken)
    {
        try
        {
            var query = new QueryPadsQuery
            {
                StartIndex = startIndex,
                Count = count,
                Filter = filter,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            var result = await _mediator.Send(query, cancellationToken);
            
            // QueryPadsQuery returns ListResponse<Pad>, we need to extract the data
            if (result is ListResponse<Pad> listResponse)
            {
                return (listResponse.Resources ?? new List<Pad>(), (int)listResponse.TotalResults);
            }

            return (new List<Pad>(), 0);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to query pads: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create a new pad
    /// </summary>
    public async Task<Guid> CreateAsync(Pad resource, CancellationToken cancellationToken)
    {
        try
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            resource.Validate();
            var command = new CreatePadCommand { Pad = resource };
            var result = await _mediator.Send(command, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create pad: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieve a pad by ID
    /// </summary>
    public async Task<Pad?> RetrieveAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetPadByIdQuery { Id = id };
            var result = await _mediator.Send(query, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve pad {id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Replace a pad completely
    /// </summary>
    public async Task<bool> ReplaceAsync(Guid id, Pad resource, CancellationToken cancellationToken)
    {
        try
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            resource.Validate();
            var command = new UpdatePadCommand { Id = id, Pad = resource };
            var result = await _mediator.Send(command, cancellationToken);
            return result != null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to replace pad {id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> ReplaceAsync(Guid id, IResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is Pad padResource)
        {
            return await ReplaceAsync(id, padResource, cancellationToken);
        }
        throw new ArgumentException($"Resource must be of type {typeof(Pad).Name}", nameof(resource));
    }

    /// <summary>
    /// Update a pad with patches
    /// </summary>
    public async Task<bool> UpdateAsync(Guid id, Pad resource, PatchOperation[] patches, CancellationToken cancellationToken)
    {
        try
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            resource.Validate();
            
            // Apply patches to resource
            if (patches != null)
            {
                foreach (var patch in patches)
                {
                    switch (patch.Op)
                    {
                        case "replace":
                            switch (patch.Path)
                            {
                                case "/name":
                                    resource.Name = patch.Value?.ToString() ?? "";
                                    break;
                                case "/active":
                                    if (patch.Value != null && bool.TryParse(patch.Value.ToString(), out var active))
                                        resource.Active = active;
                                    break;
                                case "/status":
                                    if (patch.Value != null && int.TryParse(patch.Value.ToString(), out var status))
                                        resource.Status = status;
                                    break;
                                case "/customFields":
                                    resource.CustomFields = patch.Value?.ToString() ?? "{}";
                                    break;
                            }
                            break;
                    }
                }
            }

            var command = new UpdatePadCommand { Id = id, Pad = resource };
            var result = await _mediator.Send(command, cancellationToken);
            return result != null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update pad {id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Delete a pad
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeletePadCommand { Id = id };
            await _mediator.Send(command, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete pad {id}: {ex.Message}", ex);
        }
    }
}

