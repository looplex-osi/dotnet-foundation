using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Entities;
using Looplex.Foundation.OAuth2;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;

namespace Looplex.Foundation.SCIMv2.Entities;

public class SCIMv2 : Service
{
  private readonly DbConnection? _db;
  private readonly IRbacService? _rbacService;
  private readonly IUserContext? _userContext;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public SCIMv2()
  {
  }

  #endregion

  public SCIMv2(IRbacService rbacService, IUserContext userContext, DbConnection db)
  {
    _db = db;
    _rbacService = rbacService;
    _userContext = userContext;
  }

  #region Query

  public async Task<ListResponse<T>> QueryAsync<T>(int page, int pageSize, string? filter,
    CancellationToken cancellationToken)
    where T : Resource, new()
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_userContext!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    if (filter == null)
    {
      throw new ArgumentNullException(nameof(filter));
    }
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    // Determine stored proc name.
    // For queries, the convention is USP_{resourceCollection}_pquery.
    // Example: for T == User, resource name is "user", and collection is "users".
    string resourceName = typeof(T).Name.ToLower();
    if (!resourceName.EndsWith("s"))
    {
      resourceName += "s";
    }

    ctx.Roles["ProcName"] = $"USP_{resourceName}_pquery";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      List<T> list = new();

      string procName = ctx.Roles["ProcName"];

      await _db!.OpenAsync(cancellationToken);
      using DbCommand command = _db.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      // Add expected parameters.
      command.Parameters.Add(CreateParameter(command, "@do_count", 0, DbType.Boolean));
      command.Parameters.Add(CreateParameter(command, "@page", page, DbType.Int32));
      command.Parameters.Add(CreateParameter(command, "@page_size", pageSize, DbType.Int32));
      command.Parameters.Add(CreateParameter(command, "@filter_active", 1, DbType.Boolean));
      command.Parameters.Add(CreateParameter(command, "@filter_name", filter ?? (object)DBNull.Value, DbType.String));
      command.Parameters.Add(CreateParameter(command, "@filter_email", DBNull.Value, DbType.String));
      command.Parameters.Add(CreateParameter(command, "@order_by", DBNull.Value, DbType.String));

      using DbDataReader? reader = await command.ExecuteReaderAsync(cancellationToken);
      while (await reader.ReadAsync(cancellationToken))
      {
        T obj = new();
        MapDataRecordToResource(reader, obj);
        list.Add(obj);
      }

      ctx.Result = new ListResponse<T>
      {
        Page = page, PageSize = pageSize, Resources = list, TotalResults = 0 // TODO
      };
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (ListResponse<T>)ctx.Result;
  }

  #endregion

  #region Create

  public async Task<Guid> CreateAsync<T>(T resource,
    CancellationToken cancellationToken) where T : Resource
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_userContext!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    // Determine stored proc name.
    // For creation, convention is USP_{resource}_create.
    string resourceName = typeof(T).Name.ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_create";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      await _db!.OpenAsync(cancellationToken);
      using DbCommand command = _db.CreateCommand();
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

  public async Task<T?> RetrieveAsync<T>(Guid id, CancellationToken cancellationToken)
    where T : Resource, new()
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_userContext!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = typeof(T).Name.ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_retrieve";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      await _db!.OpenAsync(cancellationToken);
      using DbCommand command = _db.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = ctx.Roles["ProcName"];

      command.Parameters.Add(CreateParameter(command, "@uuid", id, DbType.Guid));

      T? obj = null;
      using DbDataReader? reader = await command.ExecuteReaderAsync(cancellationToken);
      if (await reader.ReadAsync(cancellationToken))
      {
        obj = new T();
        MapDataRecordToResource(reader, obj);
      }

      ctx.Result = obj;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (T?)ctx.Result;
  }

  #endregion

  #region Update

  public async Task<bool> UpdateAsync<T>(Guid id, T resource, string? fields, CancellationToken cancellationToken)
    where T : Resource
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_userContext!, GetType().Name, this.GetCallerName());
    
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = typeof(T).Name.ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_update";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      await _db!.OpenAsync(cancellationToken);
      using DbCommand command = _db.CreateCommand();
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

  public async Task<bool> DeleteAsync<T>(Guid id, CancellationToken cancellationToken)
    where T : Resource
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_userContext!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    string resourceName = typeof(T).Name.ToLower();
    ctx.Roles["ProcName"] = $"USP_{resourceName}_delete";
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      await _db!.OpenAsync(cancellationToken);
      using DbCommand command = _db.CreateCommand();
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

  private static IDbDataParameter CreateParameter(IDbCommand command, string name, object value, DbType dbType)
  {
    IDbDataParameter param = command.CreateParameter();
    param.ParameterName = name;
    param.Value = value;
    param.DbType = dbType;
    return param;
  }

  private static void MapDataRecordToResource<T>(IDataRecord record, T resource)
  {
    // For each column, try to map the value to a public property with the same name.
    for (int i = 0; i < record.FieldCount; i++)
    {
      string columnName = record.GetName(i);
      PropertyInfo? prop = typeof(T).GetProperty(columnName,
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
      if (prop != null && !Convert.IsDBNull(record[i]))
      {
        try
        {
          object convertedValue = Convert.ChangeType(record[i], prop.PropertyType);
          prop.SetValue(resource, convertedValue);
        }
        catch
        {
          // Optionally, log or handle conversion errors.
        }
      }
    }
  }

  private static TProp? GetPropertyValue<TProp>(object resource, string propertyName)
  {
    PropertyInfo? prop = resource.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
    if (prop != null)
    {
      object? val = prop.GetValue(resource);
      if (val is TProp castVal)
      {
        return castVal;
      }

      return (TProp)Convert.ChangeType(val, typeof(TProp));
    }

    return default;
  }

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