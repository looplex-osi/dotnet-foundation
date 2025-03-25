using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Entities;
using Looplex.OpenForExtension.Abstractions.Plugins;

namespace Looplex.Foundation.SCIMv2.Entities;

public abstract class SCIMv2<T> : Service
  where T : Resource, new()
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public SCIMv2() : base()
  {
  }

  #endregion
  
  public SCIMv2(IList<IPlugin> plugins) : base(plugins)
  {
  }
  
  public abstract Task<ListResponse<T>> Query(int startIndex, int count,
    string? filter, string? sortBy, string? sortOrder,
    CancellationToken cancellationToken);

  public abstract Task<Guid> Create(T resource, CancellationToken cancellationToken);

  public abstract Task<T?> Retrieve(Guid id, CancellationToken cancellationToken);

  public abstract Task<bool> Update(Guid id, T resource, string? fields, CancellationToken cancellationToken);

  public abstract Task<bool> Delete(Guid id, CancellationToken cancellationToken);
  
  protected virtual IDbDataParameter CreateParameter(IDbCommand command, string name, object value, DbType dbType)
  {
    IDbDataParameter param = command.CreateParameter();
    param.ParameterName = name;
    param.Value = value;
    param.DbType = dbType;
    return param;
  }

  protected virtual void MapDataRecordToResource(IDataRecord record, T resource)
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

  protected virtual TProp? GetPropertyValue<TProp>(object resource, string propertyName)
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
}