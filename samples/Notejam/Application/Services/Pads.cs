using System.Security.Claims;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Looplex.Samples.Application.Services;

public class Pads : SCIMv2<Pad, Pad>
{
    private readonly PadsScimService _scimService;
    private readonly IMediator? _mediator;

    #region Reflectivity

    // ReSharper disable once PublicConstructorInAbstractClass
    public Pads() : base()
    {
        // This constructor is only used for reflection, not for actual service instantiation
        _scimService = null!;
        _mediator = null;
    }

    #endregion

    [ActivatorUtilitiesConstructor]
    public Pads(IList<IPlugin> plugins, IRbacService rbacService, IHttpContextAccessor httpContextAccessor,
        IMediator mediator) : base(plugins)
    {
        _mediator = mediator;
        _scimService = new PadsScimService(plugins, rbacService, httpContextAccessor.HttpContext?.User, mediator);
    }

    #region Query

    public override async Task<ListResponse<Pad>> Query(int startIndex, int count,
        string? filter, string? sortBy, string? sortOrder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        ((IDictionary<string, object>)context.State)["action"] = "Query";
        ((IDictionary<string, object>)context.State)["startIndex"] = startIndex;
        ((IDictionary<string, object>)context.State)["count"] = count;
        ((IDictionary<string, object>)context.State)["filter"] = filter ?? string.Empty;
        ((IDictionary<string, object>)context.State)["sortBy"] = sortBy ?? string.Empty;
        ((IDictionary<string, object>)context.State)["sortOrder"] = sortOrder ?? string.Empty;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        if (context.Result == null)
        {
            // Return empty result if no data was processed
            return new ListResponse<Pad>
            {
                StartIndex = startIndex,
                ItemsPerPage = count,
                Resources = new List<Pad>(),
                TotalResults = 0
            };
        }

        return (ListResponse<Pad>)context.Result;
    }

    #endregion

    #region Create

    public override async Task<Guid> Create(Pad resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        ((IDictionary<string, object>)context.State)["action"] = "Create";
        context.Roles["Pad"] = resource;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        return (Guid)context.Result;
    }

    #endregion

    #region Retrieve

    public override async Task<Pad?> Retrieve(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        ((IDictionary<string, object>)context.State)["action"] = "Retrieve";
        context.Roles["Id"] = id;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        return (Pad?)context.Result;
    }

    #endregion

    #region Replace

    public override async Task<bool> Replace(Guid id, Pad resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        ((IDictionary<string, object>)context.State)["action"] = "Update";
        context.Roles["Id"] = id;
        context.Roles["Pad"] = resource;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        return (bool)context.Result;
    }

    #endregion

    #region Update

    public override async Task<bool> Update(Guid id, Pad resource, JArray patches, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var context = _scimService.NewContext();
        ((IDictionary<string, object>)context.State)["action"] = "Update";
        context.Roles["Id"] = id;
        context.Roles["Pad"] = resource;
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
        ((IDictionary<string, object>)context.State)["action"] = "Delete";
        context.Roles["Id"] = id;

        await _scimService.ExecutePluginPipelineAsync(context, cancellationToken);

        return (bool)context.Result;
    }

    #endregion
}
