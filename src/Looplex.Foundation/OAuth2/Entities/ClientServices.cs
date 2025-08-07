using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
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

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto.Generators;

namespace Looplex.Foundation.OAuth2.Entities;

public class ClientServices : SCIMv2<ClientService, ClientService>
{
  private readonly IRbacService? _rbacService;
  private readonly ClaimsPrincipal? _user;
  private readonly IMediator? _mediator;
  private readonly IConfiguration? _configuration;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public ClientServices() : base()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public ClientServices(IList<IPlugin> plugins, IRbacService rbacService, IHttpContextAccessor httpContextAccessor,
    IMediator mediator, IConfiguration configuration) : base(plugins)
  {
    _rbacService = rbacService;
    _user = httpContextAccessor.HttpContext.User;
    _mediator = mediator;
    _configuration = configuration;
  }

  #region Query

  public override async Task<ListResponse<ClientService>> Query(int startIndex, int count,
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
      var query = new QueryResource<ClientService>(page, count, filter, sortBy, sortOrder);

      var (result, totalResults) = await _mediator!.Send(query, cancellationToken);

      ctx.Result = new ListResponse<ClientService>
      {
        StartIndex = startIndex,
        ItemsPerPage = count,
        Resources = result,
        TotalResults = totalResults
      };
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (ListResponse<ClientService>)ctx.Result;
  }

  #endregion

  #region Create

  public override async Task<Guid> Create(ClientService resource,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["ClientService"] = resource;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var clientService = ctx.Roles["ClientService"];

      clientService.Digest = await DigestCredentials(clientService.ClientSecret)!;

      var command = new CreateResource<ClientService>(clientService);

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

  public override async Task<ClientService?> Retrieve(Guid id, CancellationToken cancellationToken)
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
      var query = new RetrieveResource<ClientService>(ctx.Roles["Id"]);

      var clientService = await _mediator!.Send(query, cancellationToken);

      ctx.Result = clientService;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (ClientService?)ctx.Result;
  }

  /// <summary>
  /// This method does not have authorization. It needs to be anonymous to validate a secret for the client.
  /// </summary>
  /// <param name="id"></param>
  /// <param name="clientSecret"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async virtual Task<ClientService?> Retrieve(Guid id, string clientSecret, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      ClientService? result = null;
      var query = new RetrieveResource<ClientService>(id);
      var clientService = await _mediator!.Send(query, cancellationToken);
      bool valid = false;

      if (clientService != null)
        valid = await VerifyCredentials(clientSecret, clientService.Digest!);

      if (valid)
        result = clientService;

      ctx.Result = result;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (ClientService?)ctx.Result;
  }
  /// <summary>
  /// Verifies a client secret against its stored digest using salt and bcrypt,
  /// performing a fixed-time (OWASP) comparison to protect against timing attacks.
  /// </summary>
  private Task<bool> VerifyCredentials(string clientSecret, string digest)
  {
    return Task.Run(() =>
    {
      byte[] clientSecretBytes = System.Text.Encoding.UTF8.GetBytes(clientSecret);

      var clientSecretDigestCost = int.Parse(_configuration!["ClientSecretDigestCost"]!);

      var parts = digest.Split(':');
      var salt = Guid.Parse(parts[0]).ToByteArray();
      var digest1 = parts[1];
      var digest2 = Convert.ToBase64String(BCrypt.Generate(clientSecretBytes, salt, clientSecretDigestCost));

      return SecureEquals(digest1,digest2);
    });
  }
  /// <summary>
  /// Performs a time-constant comparison to prevent timing attacks.
  /// </summary>
  private static bool SecureEquals(string expected, string actual)
  {
    if (expected == null || actual == null)
      return false;

    if (expected.Length != actual.Length)
      return false;

    int result = 0;
    for (int i = 0; i < expected.Length; i++)
    {
      result |= expected[i] ^ actual[i];
    }

    return result == 0;
  }


  #endregion

  #region Replace

  public override async Task<bool> Replace(Guid id, ClientService resource, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = nameof(ClientService).ToLower();
    ctx.Roles["Id"] = id;
    ctx.Roles["ClientService"] = resource;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var command = new ReplaceResource<ClientService>(ctx.Roles["Id"], ctx.Roles["ClientService"]);

      var rows = await _mediator!.Send(command, cancellationToken);

      ctx.Result = rows > 0;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (bool)ctx.Result;
  }

  #endregion

  #region Update

  public override async Task<bool> Update(Guid id, ClientService resource, JArray patches, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = nameof(ClientService).ToLower();
    ctx.Roles["Id"] = id;
    ctx.Roles["ClientService"] = resource;
    ctx.Roles["Patches"] = patches;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var command = new UpdateResource<ClientService>(ctx.Roles["Id"], ctx.Roles["ClientService"], ctx.Roles["Patches"]);

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
      var command = new DeleteResource<ClientService>(ctx.Roles["Id"]);

      var rows = await _mediator!.Send(command, cancellationToken);

      ctx.Result = rows > 0;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (bool)ctx.Result;
  }

  #endregion
}