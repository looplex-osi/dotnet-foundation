using System.Security.Claims;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.Samples.Application.Commands;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

using Newtonsoft.Json.Linq;

namespace Looplex.Samples.Application.Services;

public class Notes : SCIMv2<Note, Note>
{
    private readonly NotesScimService _scimService;
    private readonly IMediator? _mediator;

    #region Reflectivity

    // ReSharper disable once PublicConstructorInAbstractClass
    public Notes() : base()
    {
        // This constructor is only used for reflection, not for actual service instantiation
        _scimService = null!;
        _mediator = null;
    }

    #endregion

    [ActivatorUtilitiesConstructor]
    public Notes(IList<IPlugin> plugins, IRbacService rbacService, IHttpContextAccessor httpContextAccessor,
        IMediator mediator) : base(plugins)
    {
        _mediator = mediator;
        _scimService = new NotesScimService(plugins, rbacService, httpContextAccessor.HttpContext?.User, mediator);
    }

    #region Query

    public override async Task<ListResponse<Note>> Query(int startIndex, int count,
        string? filter, string? sortBy, string? sortOrder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        ((IDictionary<string, object>)context.State)["startIndex"] = startIndex;
        ((IDictionary<string, object>)context.State)["count"] = count;
        ((IDictionary<string, object>)context.State)["filter"] = filter ?? string.Empty;
        ((IDictionary<string, object>)context.State)["sortBy"] = sortBy ?? string.Empty;
        ((IDictionary<string, object>)context.State)["sortOrder"] = sortOrder ?? string.Empty;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        if (context.Result == null)
        {
            // Return empty result if no data was processed
            return new ListResponse<Note>
            {
                StartIndex = startIndex,
                ItemsPerPage = count,
                Resources = new List<Note>(),
                TotalResults = 0
            };
        }

        return (ListResponse<Note>)context.Result;
    }

    #endregion

    #region Create

    public override async Task<Guid> Create(Note resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        context.Roles["Note"] = resource;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        return (Guid)context.Result;
    }

    #endregion

    #region Retrieve

    public override async Task<Note?> Retrieve(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        context.Roles["Id"] = id;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        return (Note?)context.Result;
    }

    #endregion

    #region Replace

    public override async Task<bool> Replace(Guid id, Note resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        context.Roles["Id"] = id;
        context.Roles["Note"] = resource;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        return (bool)context.Result;
    }

    #endregion

    #region Update

    public override async Task<bool> Update(Guid id, Note resource, JArray patches, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        context.Roles["Id"] = id;
        context.Roles["Note"] = resource;
        context.Roles["Patches"] = patches;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        return (bool)context.Result;
    }

    #endregion

    #region Delete

    public override async Task<bool> Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        context.Roles["Id"] = id;
        context.Roles["Delete"] = true; // Add Delete role to distinguish from Retrieve

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        return (bool)context.Result;
    }

    #endregion
}

/// <summary>
/// Concrete implementation of BaseScimService for Notes
/// </summary>
public class NotesScimService : BaseScimService
{
    private readonly IMediator _mediator;

    public NotesScimService(IList<IPlugin> plugins, IRbacService? rbacService, ClaimsPrincipal? user, IMediator mediator) 
        : base(plugins, rbacService, user)
    {
        _mediator = mediator;
    }

    protected override async Task ExecuteCustomActionAsync(IContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (_mediator == null)
            {
                throw new InvalidOperationException("IMediator is not available");
            }

            // Log context state for debugging
            var roles = string.Join(", ", context.Roles.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            var stateKeys = string.Join(", ", ((IDictionary<string, object>)context.State).Keys);

            // Determine operation type based on context roles and state
            var stateDict = (IDictionary<string, object>)context.State;
            
            // Check for Query operation first (has startIndex in state)
            if (stateDict.ContainsKey("startIndex"))
            {
                var startIndex = (int)stateDict["startIndex"];
                var count = (int)stateDict["count"];
                var filter = (string)stateDict["filter"];
                var sortBy = stateDict["sortBy"] as string;
                var sortOrder = stateDict["sortOrder"] as string;
                var page = Page(startIndex, count);

                var query = new QueryResource<Note>(page, count, filter, sortBy ?? string.Empty, sortOrder ?? string.Empty);
                var (result, totalResults) = await _mediator.Send(query, cancellationToken);

                context.Result = new ListResponse<Note>
                {
                    StartIndex = startIndex,
                    ItemsPerPage = count,
                    Resources = result ?? new List<Note>(),
                    TotalResults = totalResults
                };
            }
            // Check for Create operation (has Note but no Id)
            else if (context.Roles.ContainsKey("Note") && !context.Roles.ContainsKey("Id"))
            {
                var command = new CreateNoteCommand((Note)context.Roles["Note"]);
                var result = await _mediator.Send(command, cancellationToken);
                context.Result = result;
            }
            // Check for Delete operation (has Id and Delete role)
            else if (context.Roles.ContainsKey("Id") && context.Roles.ContainsKey("Delete"))
            {
                var id = (Guid)context.Roles["Id"];
                var command = new DeleteNoteCommand(id);
                var rows = await _mediator.Send(command, cancellationToken);
                context.Result = rows > 0;
            }
            // Check for Replace operation (has Id and Note)
            else if (context.Roles.ContainsKey("Id") && context.Roles.ContainsKey("Note"))
            {
                var id = (Guid)context.Roles["Id"];
                var note = (Note)context.Roles["Note"];
                
                if (context.Roles.ContainsKey("Patches"))
                {
                    // Update operation
                    var command = new UpdateNoteCommand(id, note, (JArray)context.Roles["Patches"]);
                    var rows = await _mediator.Send(command, cancellationToken);
                    context.Result = rows > 0;
                }
                else
                {
                    // Replace operation
                    var command = new UpdateNoteCommand(id, note);
                    var rows = await _mediator.Send(command, cancellationToken);
                    context.Result = rows > 0;
                }
            }
            // Check for Retrieve operation (has Id but no Note and no Delete)
            else if (context.Roles.ContainsKey("Id") && !context.Roles.ContainsKey("Note") && !context.Roles.ContainsKey("Delete"))
            {
                var id = (Guid)context.Roles["Id"];
                var query = new RetrieveResource<Note>(id);
                var result = await _mediator.Send(query, cancellationToken);
                context.Result = result;
            }
        }
        catch (Exception)
        {
            // Log the exception and set a default result
            context.Result = null;
            throw;
        }
    }
}