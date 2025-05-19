using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.SCIMv2.Entities;

public class Users : SCIMv2<User, User>
{
  private readonly IRbacService? _rbacService;
  private readonly ClaimsPrincipal? _user;
  private readonly IMediator? _mediator;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public Users() : base()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public Users(IList<IPlugin> plugins, IRbacService rbacService, IHttpContextAccessor httpContextAccessor, IMediator mediator) : base(plugins)
  {
    _rbacService = rbacService;
    _user = httpContextAccessor.HttpContext.User;
    _mediator = mediator;
  }

  #region Query

  public override async Task<ListResponse<User>> Query(int startIndex, int count,
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
      var query = new QueryResource<User>(page, count, filter, sortBy, sortOrder);

      var (result, totalResults) = await _mediator!.Send(query, cancellationToken);

      ctx.Result = new ListResponse<User>
      {
        StartIndex = startIndex,
        ItemsPerPage = count,
        Resources = result,
        TotalResults = totalResults
      };
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (ListResponse<User>)ctx.Result;
  }

  #endregion

  #region Create

  public override async Task<Guid> Create(User resource,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["User"] = resource;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var command = new CreateResource<User>(ctx.Roles["User"]);

      var result = await _mediator!.Send(command, cancellationToken);

      ctx.Result = result;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (Guid)ctx.Result;
  }

  #endregion

  #region Retrieve

  public override async Task<User?> Retrieve(Guid id, CancellationToken cancellationToken)
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
      var query = new RetrieveResource<User>(ctx.Roles["Id"]);

      var user = await _mediator!.Send(query, cancellationToken);

      ctx.Result = user;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (User?)ctx.Result;
  }

  #endregion

  #region Replace

  public override async Task<bool> Replace(Guid id, User resource, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = nameof(User).ToLower();
    ctx.Roles["Id"] = id;
    ctx.Roles["User"] = resource;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var command = new ReplaceResource<User>(ctx.Roles["Id"], ctx.Roles["User"]);

      var rows = await _mediator!.Send(command, cancellationToken);

      ctx.Result = rows > 0;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (bool)ctx.Result;
  }

  #endregion

  #region Update

  public override async Task<bool> Update(Guid id, User resource, JArray patches, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = nameof(User).ToLower();
    ctx.Roles["Id"] = id;
    ctx.Roles["User"] = resource;
    ctx.Roles["Patches"] = patches;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var command = new UpdateResource<User>(ctx.Roles["Id"], ctx.Roles["User"], ctx.Roles["Patches"]);

      var rows = await _mediator!.Send(command, cancellationToken);

      ctx.Result = rows > 0;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (bool)ctx.Result;
  }

  #endregion

  #region Delete

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
      var command = new DeleteResource<User>(ctx.Roles["Id"]);

      var rows = await _mediator!.Send(command, cancellationToken);

      ctx.Result = rows > 0;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (bool)ctx.Result;
  }

  #endregion
}