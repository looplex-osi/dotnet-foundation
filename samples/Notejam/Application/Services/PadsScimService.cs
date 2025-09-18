using System.Security.Claims;
using System.Collections.Generic;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application.Queries;
using Looplex.Samples.Application.Commands;
using MediatR;

namespace Looplex.Samples.Application.Services;

/// <summary>
/// SCIM service that handles HTTP requests for Pad resources (GET, POST, PUT, PATCH, DELETE).
/// Routes requests to appropriate MediatR handlers and manages SCIM protocol compliance.
/// </summary>
public class PadsScimService : BaseScimService
{
    private readonly IMediator _mediator;

    public PadsScimService(IList<IPlugin> plugins, IRbacService rbacService, ClaimsPrincipal? user, IMediator mediator) 
        : base(plugins, rbacService, user)
    {
        _mediator = mediator;
    }

    protected override async Task ExecuteCustomActionAsync(IContext context, CancellationToken cancellationToken)
    {
        var stateDict = (IDictionary<string, object>)context.State;
        var action = stateDict.ContainsKey("action") ? stateDict["action"] as string : null;

        switch (action)
        {
            case "Query":
                await Query(context, cancellationToken);
                break;
            case "Create":
                await Create(context, cancellationToken);
                break;
            case "Retrieve":
                await Retrieve(context, cancellationToken);
                break;
            case "Update":
                await Update(context, cancellationToken);
                break;
            case "Delete":
                await Delete(context, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unknown action: {action}");
        }
    }

    private async Task Query(IContext context, CancellationToken cancellationToken)
    {
        var stateDict = (IDictionary<string, object>)context.State;
        var startIndex = stateDict.ContainsKey("startIndex") ? stateDict["startIndex"] as int? ?? 1 : 1;
        var count = stateDict.ContainsKey("count") ? stateDict["count"] as int? ?? 12 : 12;
        var filter = stateDict.ContainsKey("filter") ? stateDict["filter"] as string : null;
        var sortBy = stateDict.ContainsKey("sortBy") ? stateDict["sortBy"] as string : null;
        var sortOrder = stateDict.ContainsKey("sortOrder") ? stateDict["sortOrder"] as string : null;

        var query = new QueryPadsQuery
        {
            StartIndex = startIndex,
            Count = count,
            Filter = filter,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _mediator.Send(query, cancellationToken);
        context.Result = result;
    }

    private async Task Create(IContext context, CancellationToken cancellationToken)
    {
        var rolesDict = (IDictionary<string, object>)context.Roles;
        var pad = rolesDict.ContainsKey("Pad") ? rolesDict["Pad"] as Pad : null;
        if (pad == null)
            throw new InvalidOperationException("Pad resource not found in context");

        var command = new CreatePadCommand { Pad = pad };
        var result = await _mediator.Send(command, cancellationToken);
        context.Result = result;
    }

    private async Task Retrieve(IContext context, CancellationToken cancellationToken)
    {
        var id = (Guid)context.Roles["Id"];
        var query = new GetPadByIdQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);
        context.Result = result;
    }

    private async Task Update(IContext context, CancellationToken cancellationToken)
    {
        var id = (Guid)context.Roles["Id"];
        var rolesDict = (IDictionary<string, object>)context.Roles;
        var pad = rolesDict.ContainsKey("Pad") ? rolesDict["Pad"] as Pad : null;
        if (pad == null)
            throw new InvalidOperationException("Pad resource not found in context");

        // Verificar se há patches para processar (PATCH operation)
        if (rolesDict.ContainsKey("Patches"))
        {
            var patches = rolesDict["Patches"] as Newtonsoft.Json.Linq.JArray;
            if (patches != null)
            {
                // Processar patches JSON
                foreach (var patch in patches)
                {
                    var op = patch["op"]?.ToString();
                    var path = patch["path"]?.ToString();
                    var value = patch["value"];

                    switch (op)
                    {
                        case "replace":
                            switch (path)
                            {
                                case "/name":
                                    pad.Name = value?.ToString() ?? "";
                                    break;
                                case "/active":
                                    if (value != null && bool.TryParse(value.ToString(), out var active))
                                        pad.Active = active;
                                    break;
                                case "/status":
                                    if (value != null && int.TryParse(value.ToString(), out var status))
                                        pad.Status = status;
                                    break;
                                case "/customFields":
                                    pad.CustomFields = value?.ToString() ?? "{}";
                                    break;
                            }
                            break;
                        // Adicionar outros operadores conforme necessário
                    }
                }
            }
        }

        var command = new UpdatePadCommand { Id = id, Pad = pad };
        var result = await _mediator.Send(command, cancellationToken);
        // Para Replace e Update, retornamos true se a operação foi bem-sucedida
        context.Result = result != null;
    }

    private async Task Delete(IContext context, CancellationToken cancellationToken)
    {
        var id = (Guid)context.Roles["Id"];
        var command = new DeletePadCommand { Id = id };
        await _mediator.Send(command, cancellationToken);
        context.Result = true;
    }
}
