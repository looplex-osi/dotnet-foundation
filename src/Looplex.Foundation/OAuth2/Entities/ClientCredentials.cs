using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Org.BouncyCastle.Crypto.Generators;

namespace Looplex.Foundation.OAuth2.Entities;

public class ClientCredentials : SCIMv2<ClientCredential>
{
  private readonly IRbacService? _rbacService;
  private readonly ClaimsPrincipal? _user;
  private readonly IMediator? _mediator;
  private readonly IConfiguration? _configuration;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public ClientCredentials() : base()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public ClientCredentials(IList<IPlugin> plugins, IRbacService rbacService, IHttpContextAccessor httpContextAccessor,
    IMediator mediator, IConfiguration configuration) : base(plugins)
  {
    _rbacService = rbacService;
    _user = httpContextAccessor.HttpContext.User;
    _mediator = mediator;
    _configuration = configuration;
  }

  #region Query

  public override async Task<ListResponse<ClientCredential>> Query(int startIndex, int count,
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
      var query = new QueryResource<ClientCredential>(page, count, filter, sortBy, sortOrder);

      var (result, totalResults) = await _mediator!.Send(query, cancellationToken);

      ctx.Result = new ListResponse<ClientCredential>
      {
        StartIndex = startIndex, ItemsPerPage = count, Resources = result, TotalResults = totalResults
      };
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (ListResponse<ClientCredential>)ctx.Result;
  }

  #endregion

  #region Create

  public override async Task<Guid> Create(ClientCredential resource,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["ClientCredential"] = resource;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var clientCredential = ctx.Roles["ClientCredential"];

      clientCredential.Digest = await DigestCredentials(clientCredential.ClientSecret)!;
      
      var command = new CreateResource<ClientCredential>(clientCredential);

      var result = await _mediator!.Send(command, cancellationToken);

      ctx.Result = result;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (Guid)ctx.Result;
  }
  
  private Task<string> DigestCredentials(string clientSecret)
  {
    return Task.Run(() =>
    {
      Guid salt = Guid.NewGuid();
      byte[] clientSecretBytes = System.Text.Encoding.UTF8.GetBytes(clientSecret);

      var clientSecretDigestCost = int.Parse(_configuration!["ClientSecretDigestCost"]!);

      string digest = Convert.ToBase64String(BCrypt.Generate(
        clientSecretBytes,
        salt.ToByteArray(),
        clientSecretDigestCost));

      return $"{salt}:{digest}";
    });
  }

  #endregion

  #region Retrieve

  public override async Task<ClientCredential?> Retrieve(Guid id, CancellationToken cancellationToken)
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
      var query = new RetrieveResource<ClientCredential>(ctx.Roles["Id"]);

      var clientCredential = await _mediator!.Send(query, cancellationToken);

      ctx.Result = clientCredential;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (ClientCredential?)ctx.Result;
  }

  public async virtual Task<ClientCredential?> Retrieve(Guid id, string clientSecret, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    var resource = await Retrieve(id, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["ClientCredential"] = resource;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      ClientCredential? result = null;
      var clientCredential = (ClientCredential)ctx.Roles["ClientCredential"];

      var valid = await VerifyCredentials(clientSecret, clientCredential.Digest!);
      if (valid)
      {
        result = clientCredential;
      }

      ctx.Result = result;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (ClientCredential?)ctx.Result;
  }
  
  private Task<bool> VerifyCredentials(string clientSecret, string digest)
  {
    return Task.Run(() =>
    {
      byte[] clientSecretBytes = System.Text.Encoding.UTF8.GetBytes(clientSecret);

      var clientSecretDigestCost = int.Parse(_configuration!["ClientSecretDigestCost"]!);

      var parts = digest.Split(':');
      var salt = Guid.Parse(parts[0]).ToByteArray();
      var digest1 = parts[1];
      var digest2 = Convert.ToBase64String(BCrypt.Generate(
        clientSecretBytes,
        salt,
        clientSecretDigestCost));

      return digest1 == digest2;
    });
  }

  #endregion

  #region Update

  public override async Task<bool> Update(Guid id, ClientCredential resource, string? fields, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = nameof(ClientCredential).ToLower();
    ctx.Roles["Id"] = id;
    ctx.Roles["ClientCredential"] = resource;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var command = new UpdateResource<ClientCredential>(ctx.Roles["Id"], ctx.Roles["ClientCredential"]);

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
      var command = new DeleteResource<ClientCredential>(ctx.Roles["Id"]);

      var rows = await _mediator!.Send(command, cancellationToken);

      ctx.Result = rows > 0;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (bool)ctx.Result;
  }

  #endregion
}