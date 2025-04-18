using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.SCIMv2.Entities;

public class Users : SCIMv2<User>
{
  private readonly IDbConnections? _connections;
  private readonly IRbacService? _rbacService;
  private readonly ClaimsPrincipal? _user;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public Users() : base()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public Users(IList<IPlugin> plugins, IRbacService rbacService, IHttpContextAccessor httpContextAccessor,
    IDbConnections connections) : base(plugins)
  {
    _connections = connections;
    _rbacService = rbacService;
    _user = httpContextAccessor.HttpContext.User;
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

    // Determine stored proc name.
    // For queries, the convention is USP_{resourceCollection}_pquery.
    // Example: for T == User, resource name is "user", and collection is "users".
    string resourceName = nameof(User).ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_pquery";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      List<User> list = new();

      string procName = ctx.Roles["ProcName"];

      var dbQuery = await _connections!.QueryConnection();
      await dbQuery!.OpenAsync(cancellationToken);
      using DbCommand command = dbQuery.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      // Add expected parameters.
      command.Parameters.Add(Dbs.CreateParameter(command, "@do_count", 0, DbType.Boolean));
      command.Parameters.Add(Dbs.CreateParameter(command, "@page", page, DbType.Int32));
      command.Parameters.Add(Dbs.CreateParameter(command, "@page_size", count, DbType.Int32));
      command.Parameters.Add(Dbs.CreateParameter(command, "@filter_active", 1, DbType.Boolean));
      command.Parameters.Add(
        Dbs.CreateParameter(command, "@filter_name", filter ?? (object)DBNull.Value, DbType.String));
      command.Parameters.Add(Dbs.CreateParameter(command, "@filter_email", DBNull.Value, DbType.String));
      command.Parameters.Add(Dbs.CreateParameter(command, "@order_by", DBNull.Value, DbType.String));

      using DbDataReader? reader = await command.ExecuteReaderAsync(cancellationToken);
      while (await reader.ReadAsync(cancellationToken))
      {
        User obj = new();
        Dbs.MapDataRecordToResource(reader, obj);
        list.Add(obj);
      }

      ctx.Result = new ListResponse<User>
      {
        StartIndex = startIndex, ItemsPerPage = count, Resources = list, TotalResults = 0 // TODO
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

    // Determine stored proc name.
    // For creation, convention is USP_{resource}_create.
    string resourceName = nameof(User).ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_create";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var dbCommand = await _connections!.CommandConnection();
      await dbCommand!.OpenAsync(cancellationToken);
      using DbCommand command = dbCommand.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = ctx.Roles["ProcName"];

      // For example, for a User resource we assume:
      // - Property "UserName" maps to @name.
      // - Property "Emails" (a collection) provides the first email's Value for @email.
      // You can customize this mapping as needed.
      string nameValue = resource.GetPropertyValue<string>("UserName")
                         ?? throw new Exception("UserName is required.");
      string emailValue = resource.GetFirstEmailValue()
                          ?? throw new Exception("At least one email is required.");

      command.Parameters.Add(Dbs.CreateParameter(command, "@name", nameValue, DbType.String));
      command.Parameters.Add(Dbs.CreateParameter(command, "@email", emailValue, DbType.String));
      // Rely on defaults for @active, @status, and @custom_fields.

      // Execute and return the new resource's GUID.
      object result = await command.ExecuteScalarAsync(cancellationToken);
      ctx.Result = (Guid)result;
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

    string resourceName = nameof(User).ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_retrieve";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var dbQuery = await _connections!.QueryConnection();
      await dbQuery!.OpenAsync(cancellationToken);
      using DbCommand command = dbQuery.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = ctx.Roles["ProcName"];

      command.Parameters.Add(Dbs.CreateParameter(command, "@uuid", id, DbType.Guid));

      User? obj = null;
      using DbDataReader? reader = await command.ExecuteReaderAsync(cancellationToken);
      if (await reader.ReadAsync(cancellationToken))
      {
        obj = new User();
        Dbs.MapDataRecordToResource(reader, obj);
      }

      ctx.Result = obj;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (User?)ctx.Result;
  }

  #endregion

  #region Update

  public override async Task<bool> Update(Guid id, User resource, string? fields, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = nameof(User).ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_update";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var dbCommand = await _connections!.CommandConnection();
      await dbCommand!.OpenAsync(cancellationToken);
      using DbCommand command = dbCommand.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = ctx.Roles["ProcName"];

      command.Parameters.Add(Dbs.CreateParameter(command, "@uuid", id, DbType.Guid));

      // Update mapping: if the resource contains a non-null UserName, update @name.
      string? nameValue = resource.GetPropertyValue<string>("UserName");
      if (!string.IsNullOrWhiteSpace(nameValue))
      {
        command.Parameters.Add(Dbs.CreateParameter(command, "@name", nameValue!, DbType.String));
      }

      // Similarly, update email if available.
      string? emailValue = resource.GetFirstEmailValue();
      if (!string.IsNullOrWhiteSpace(emailValue))
      {
        command.Parameters.Add(Dbs.CreateParameter(command, "@email", emailValue!, DbType.String));
      }
      // Additional parameters (active, status, custom_fields) can be mapped as needed.

      int rows = await command.ExecuteNonQueryAsync(cancellationToken);

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

    string resourceName = nameof(User).ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_delete";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var dbCommand = await _connections!.CommandConnection();
      await dbCommand!.OpenAsync(cancellationToken);
      using DbCommand command = dbCommand.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = ctx.Roles["ProcName"];

      command.Parameters.Add(Dbs.CreateParameter(command, "@uuid", id, DbType.Guid));

      // If your stored procedure supported a parameter for hard deletion,
      // you could add it here. For now, we assume the same proc handles deletion.
      int rows = await command.ExecuteNonQueryAsync(cancellationToken);

      ctx.Result = rows > 0;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (bool)ctx.Result;
  }

  #endregion
}