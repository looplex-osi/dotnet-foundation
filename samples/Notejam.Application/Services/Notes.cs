using System.Collections;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Security.Claims;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.Samples.Domain.Entities;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Samples.Application.Services;

public class Notes : SCIMv2<Note>
{
  private readonly IRbacService? _rbacService;
  private readonly ClaimsPrincipal? _user;
  private readonly DbConnection? _dbCommand;
  private readonly DbConnection? _dbQuery;
  private readonly IMediator? _mediator;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public Notes() : base()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public Notes(IList<IPlugin> plugins, IRbacService rbacService, IHttpContextAccessor httpContextAccessor,
    DbConnection dbCommand, DbConnection dbQuery, IMediator mediator) : base(plugins)
  {
    _dbCommand = dbCommand;
    _dbQuery = dbQuery;
    _rbacService = rbacService;
    _user = httpContextAccessor.HttpContext.User;
    _mediator = mediator;
  }

  #region Query

  public override async Task<ListResponse<Note>> Query(int startIndex, int count,
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
      List<Note> list = new();
      string resourceName = nameof(Note).ToLower();
      string procName = $"USP_{resourceName}_pquery";

      await _dbQuery!.OpenAsync(cancellationToken);
      using DbCommand command = _dbQuery.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      // Add expected parameters.
      command.Parameters.Add(CreateParameter(command, "@do_count", 0, DbType.Boolean));
      command.Parameters.Add(CreateParameter(command, "@page", page, DbType.Int32));
      command.Parameters.Add(CreateParameter(command, "@page_size", count, DbType.Int32));
      command.Parameters.Add(CreateParameter(command, "@filter_active", 1, DbType.Boolean));
      command.Parameters.Add(CreateParameter(command, "@filter_name", filter ?? (object)DBNull.Value, DbType.String));
      command.Parameters.Add(CreateParameter(command, "@filter_email", DBNull.Value, DbType.String));
      command.Parameters.Add(CreateParameter(command, "@order_by", DBNull.Value, DbType.String));

      using DbDataReader? reader = await command.ExecuteReaderAsync(cancellationToken);
      while (await reader.ReadAsync(cancellationToken))
      {
        Note obj = new();
        MapDataRecordToResource(reader, obj);
        list.Add(obj);
      }

      ctx.Result = new ListResponse<Note>
      {
        StartIndex = startIndex, ItemsPerPage = count, Resources = list, TotalResults = 0 // TODO
      };
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (ListResponse<Note>)ctx.Result;
  }

  #endregion

  #region Create

  public override async Task<Guid> Create(Note resource,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = nameof(Note).ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_create";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      await _dbCommand!.OpenAsync(cancellationToken);
      using DbCommand command = _dbCommand.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = ctx.Roles["ProcName"];

      // For example, for a User resource we assume:
      // - Property "UserName" maps to @name.
      // - Property "Emails" (a collection) provides the first email's Value for @email.
      // You can customize this mapping as needed.
      string nameValue = GetPropertyValue<string>(resource, "UserName")
                         ?? throw new Exception("UserName is required.");
      string emailValue = GetFirstEmailValue(resource)
                          ?? throw new Exception("At least one email is required.");

      command.Parameters.Add(CreateParameter(command, "@name", nameValue, DbType.String));
      command.Parameters.Add(CreateParameter(command, "@email", emailValue, DbType.String));
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

  public override async Task<Note?> Retrieve(Guid id, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = nameof(Note).ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_retrieve";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      await _dbQuery!.OpenAsync(cancellationToken);
      using DbCommand command = _dbQuery.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = ctx.Roles["ProcName"];

      command.Parameters.Add(CreateParameter(command, "@uuid", id, DbType.Guid));

      Note? obj = null;
      using DbDataReader? reader = await command.ExecuteReaderAsync(cancellationToken);
      if (await reader.ReadAsync(cancellationToken))
      {
        obj = new Note();
        MapDataRecordToResource(reader, obj);
      }

      ctx.Result = obj;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (Note?)ctx.Result;
  }

  #endregion

  #region Update

  public override async Task<bool> Update(Guid id, Note resource, string? fields, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = nameof(Note).ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_update";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      await _dbCommand!.OpenAsync(cancellationToken);
      using DbCommand command = _dbCommand.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = ctx.Roles["ProcName"];

      command.Parameters.Add(CreateParameter(command, "@uuid", id, DbType.Guid));

      // Update mapping: if the resource contains a non-null UserName, update @name.
      string? nameValue = GetPropertyValue<string>(resource, "UserName");
      if (!string.IsNullOrWhiteSpace(nameValue))
      {
        command.Parameters.Add(CreateParameter(command, "@name", nameValue!, DbType.String));
      }

      // Similarly, update email if available.
      string? emailValue = GetFirstEmailValue(resource);
      if (!string.IsNullOrWhiteSpace(emailValue))
      {
        command.Parameters.Add(CreateParameter(command, "@email", emailValue!, DbType.String));
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

    string resourceName = nameof(Note).ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_delete";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      await _dbCommand!.OpenAsync(cancellationToken);
      using DbCommand command = _dbCommand.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = ctx.Roles["ProcName"];

      command.Parameters.Add(CreateParameter(command, "@uuid", id, DbType.Guid));

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

  #region Helper Methods

  private static string? GetFirstEmailValue(object resource)
  {
    // Assume resource has a property named "Emails" that is an IEnumerable.
    PropertyInfo? emailsProp = resource.GetType().GetProperty("Emails", BindingFlags.Public | BindingFlags.Instance);
    if (emailsProp == null)
    {
      return null;
    }

    if (emailsProp.GetValue(resource) is IEnumerable emails)
    {
      foreach (object? item in emails)
      {
        // Assume each email item has a property "Value".
        PropertyInfo? valueProp = item.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        if (valueProp != null)
        {
          object? val = valueProp.GetValue(item);
          if (val != null)
          {
            return val.ToString();
          }
        }
      }
    }

    return null;
  }

  #endregion
}