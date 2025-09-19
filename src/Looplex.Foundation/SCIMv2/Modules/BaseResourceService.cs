using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.SCIMv2.Modules;

/// <summary>
/// Generic base class to eliminate duplication between Users.cs and Groups.cs
/// Maintains current structure, only eliminates duplicated code
/// 
/// Implements RFC 7644 (SCIM Protocol) for resource management
/// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644) - SCIM Protocol
/// 
/// RFC Compliance:
/// - RFC 7644 Section 3.4.1 - Create Resource
/// - RFC 7644 Section 3.4.2 - Query Resources
/// - RFC 7644 Section 3.4.3 - Retrieve Resource
/// - RFC 7644 Section 3.4.4 - Update Resource
/// - RFC 7644 Section 3.4.5 - Delete Resource
/// </summary>
public abstract class BaseResourceService<T> : SCIMv2<T, T> where T : Resource, new()
{
    protected readonly IRbacService? _rbacService;
    protected readonly ClaimsPrincipal? _user;
    protected readonly IMediator? _mediator;

    #region Constructors

    /// <summary>
    /// Default constructor for reflection
    /// </summary>
    protected BaseResourceService() : base()
    {
    }

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    [ActivatorUtilitiesConstructor]
    protected BaseResourceService(
        IList<IPlugin> plugins, 
        IRbacService rbacService, 
        IHttpContextAccessor httpContextAccessor,
        IMediator mediator) : base(plugins)
    {
        _rbacService = rbacService;
        _user = httpContextAccessor.HttpContext.User;
        _mediator = mediator;
    }

    #endregion

    #region Common CRUD Operations

    /// <summary>
    /// Query resources with common SCIMv2 logic
    /// Eliminates duplication between Users.cs and Groups.cs
    /// </summary>
    public override async Task<ListResponse<T>> Query(int startIndex, int count,
        string? filter, string? sortBy, string? sortOrder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IContext ctx = NewContext();
        _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

        int page = Page(startIndex, count);

        await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

        if (!ctx.SkipDefaultAction)
        {
            var query = new QueryResource<T>(page, count, filter, sortBy, sortOrder);

            var queryResult = await _mediator!.Send(query, cancellationToken);
            
            ctx.Result = new ListResponse<T>
            {
                StartIndex = startIndex,
                ItemsPerPage = count,
                Resources = queryResult.Item1 ?? new List<T>(),
                TotalResults = queryResult.Item2
            };
        }

        await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

        return (ListResponse<T>)ctx.Result;
    }

    /// <summary>
    /// Create resource with common SCIMv2 logic
    /// Eliminates duplication between Users.cs and Groups.cs
    /// </summary>
    public override async Task<Guid> Create(T resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IContext ctx = NewContext();
        _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

        await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

        // Apply business rules specific to the resource type
        resource = await ApplyBusinessRulesAsync(resource, cancellationToken);

        // Validate resource specific to the resource type
        var isValid = await ValidateResourceAsync(resource, cancellationToken);
        if (!isValid)
        {
            throw new ArgumentException("Resource validation failed");
        }

        ctx.Roles[GetResourceRoleName()] = resource;
        await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

        if (!ctx.SkipDefaultAction)
        {
            var command = new CreateResource<T>((T)ctx.Roles[GetResourceRoleName()]);

            var result = await _mediator!.Send(command, cancellationToken);

            ctx.Result = result;
        }

        await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

        return (Guid)ctx.Result;
    }

    /// <summary>
    /// Retrieve resource with common SCIMv2 logic
    /// Eliminates duplication between Users.cs and Groups.cs
    /// </summary>
    public override async Task<T?> Retrieve(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IContext ctx = NewContext();
        _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

        await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

        ctx.Roles["Id"] = id;
        await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

        if (!ctx.SkipDefaultAction)
        {
            var query = new RetrieveResource<T>((Guid)ctx.Roles["Id"]);

            var resource = await _mediator!.Send(query, cancellationToken);

            ctx.Result = resource;
        }

        await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

        return (T?)ctx.Result;
    }

    /// <summary>
    /// Replace resource with common SCIMv2 logic
    /// Eliminates duplication between Users.cs and Groups.cs
    /// </summary>
    public override async Task<bool> Replace(Guid id, T resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IContext ctx = NewContext();
        _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

        await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

        // Apply business rules specific to the resource type
        resource = await ApplyBusinessRulesAsync(resource, cancellationToken);

        // Validate resource specific to the resource type
        var isValid = await ValidateResourceAsync(resource, cancellationToken);
        if (!isValid)
        {
            throw new ArgumentException("Resource validation failed");
        }

        string resourceName = GetResourceTypeName().ToLower();
        ctx.Roles["Id"] = id;
        ctx.Roles[GetResourceRoleName()] = resource;
        await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

        if (!ctx.SkipDefaultAction)
        {
            var command = new ReplaceResource<T>((Guid)ctx.Roles["Id"], (T)ctx.Roles[GetResourceRoleName()]);

            var rows = await _mediator!.Send(command, cancellationToken);

            ctx.Result = rows > 0;
        }

        await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

        return (bool)ctx.Result;
    }

    /// <summary>
    /// Update resource with common SCIMv2 logic
    /// Eliminates duplication between Users.cs and Groups.cs
    /// </summary>
    public override async Task<bool> Update(Guid id, T resource, JArray patches, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IContext ctx = NewContext();
        _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

        await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

        string resourceName = GetResourceTypeName().ToLower();
        ctx.Roles["Id"] = id;
        ctx.Roles[GetResourceRoleName()] = resource;
        ctx.Roles["Patches"] = patches;
        await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

        if (!ctx.SkipDefaultAction)
        {
            var command = new UpdateResource<T>((Guid)ctx.Roles["Id"], (T)ctx.Roles[GetResourceRoleName()], (JArray)ctx.Roles["Patches"]);

            var rows = await _mediator!.Send(command, cancellationToken);

            ctx.Result = rows > 0;
        }

        await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

        return (bool)ctx.Result;
    }

    /// <summary>
    /// Delete resource with common SCIMv2 logic
    /// Eliminates duplication between Users.cs and Groups.cs
    /// </summary>
    public override async Task<bool> Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IContext ctx = NewContext();
        _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

        await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

        ctx.Roles["Id"] = id;
        await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

        if (!ctx.SkipDefaultAction)
        {
            var command = new DeleteResource<T>((Guid)ctx.Roles["Id"]);

            var rows = await _mediator!.Send(command, cancellationToken);

            ctx.Result = rows > 0;
        }

        await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

        return (bool)ctx.Result;
    }

    #endregion

    #region Abstract Methods for Customization

    /// <summary>
    /// Apply business rules specific to the resource type
    /// Override in derived classes for custom business logic
    /// </summary>
    protected abstract Task<T> ApplyBusinessRulesAsync(T resource, CancellationToken cancellationToken);

    /// <summary>
    /// Validate resource specific to the resource type
    /// Override in derived classes for custom validation logic
    /// </summary>
    protected abstract Task<bool> ValidateResourceAsync(T resource, CancellationToken cancellationToken);

    /// <summary>
    /// Get the resource role name for context
    /// Override in derived classes for custom role names
    /// </summary>
    protected abstract string GetResourceRoleName();

    /// <summary>
    /// Get the resource type name
    /// Override in derived classes for custom type names
    /// </summary>
    protected abstract string GetResourceTypeName();

    #endregion

    #region Command/Query Factory Methods

    /// <summary>
    /// Create query resource command
    /// Override in derived classes for custom query logic
    /// </summary>
    protected abstract object CreateQueryResource(int page, int count, string? filter, string? sortBy, string? sortOrder);

    /// <summary>
    /// Create create resource command
    /// Override in derived classes for custom create logic
    /// </summary>
    protected abstract object CreateCreateResourceCommand(object resource);

    /// <summary>
    /// Create retrieve resource query
    /// Override in derived classes for custom retrieve logic
    /// </summary>
    protected abstract object CreateRetrieveResourceQuery(object id);

    /// <summary>
    /// Create replace resource command
    /// Override in derived classes for custom replace logic
    /// </summary>
    protected abstract object CreateReplaceResourceCommand(object id, object resource);

    /// <summary>
    /// Create update resource command
    /// Override in derived classes for custom update logic
    /// </summary>
    protected abstract object CreateUpdateResourceCommand(object id, object resource, object patches);

    /// <summary>
    /// Create delete resource command
    /// Override in derived classes for custom delete logic
    /// </summary>
    protected abstract object CreateDeleteResourceCommand(object id);

    #endregion
}
