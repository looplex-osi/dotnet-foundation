using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.SCIMv2.Entities;

namespace Looplex.Foundation.Helpers;

public static class Dbs
{
  public const string TotalCount = "TOTAL_COUNT";

  public static IDbDataParameter CreateParameter(IDbCommand command, string name, object value, DbType dbType)
  {
    IDbDataParameter param = command.CreateParameter();
    param.ParameterName = name;
    param.Value = value;
    param.DbType = dbType;
    return param;
  }

  public static void MapDataRecordToResource<T>(IDataRecord record, T resource)
    where T : Resource, new()
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

  public static object? GetValue(this DataRow row, string columnName)
  {
    (object? value, _) = row.GetColumnValue(columnName);

    return value;
  }

  public static SqlResultSet[] GetResultSet(this IEnumerable<SqlResultSet> allResultSets, string tableName)
  {
    IEnumerable<SqlResultSet> res =
      allResultSets.Where(resultSet => resultSet.Result?.TableName.Contains(tableName) ?? false);

    return res.ToArray();
  }

  private static (object? value, Type? tipo) GetColumnValue(this DataRow row, string columnName)
  {
    DataColumn? col = row.Table.Columns[columnName];

    if (col is null)
    {
      return (null, null);
    }

    string? value = row[columnName].ToString();

    if (col.DataType == typeof(DateTime) || col.DataType == typeof(string) && DateTime.TryParse(value, out DateTime _))
    {
      bool converted = DateTime.TryParse(value, out DateTime d);

      if (!converted || d == DateTime.MinValue)
      {
        return (null, col.DataType);
      }
      else
      {
        return (d, col.DataType);
      }
    }
    else if (col.DataType == typeof(string))
    {
      return (value, col.DataType);
    }
    else if (col.DataType == typeof(short))
    {
      bool converted = int.TryParse(value, out int s);

      if (!converted)
      {
        return (int.MinValue, col.DataType);
      }
      else
      {
        return (s, typeof(int));
      }
    }
    else if (col.DataType == typeof(int))
    {
      bool converted = int.TryParse(value, out int i);

      if (!converted)
      {
        return (int.MinValue, col.DataType);
      }
      else
      {
        return (i, col.DataType);
      }
    }
    else if (col.DataType == typeof(bool))
    {
      bool converted = bool.TryParse(value, out bool b);

      if (!converted)
      {
        return (false, col.DataType);
      }
      else
      {
        return (b, col.DataType);
      }
    }
    else if (col.DataType == typeof(decimal))
    {
      bool converted = decimal.TryParse(value, out decimal d);

      if (!converted)
      {
        return (decimal.MinValue, col.DataType);
      }
      else
      {
        return (d, col.DataType);
      }
    }
    else if (col.DataType == typeof(float))
    {
      bool converted = float.TryParse(value, out float f);

      if (!converted)
      {
        return (float.MinValue, col.DataType);
      }
      else
      {
        return (f, col.DataType);
      }
    }
    else if (col.DataType == typeof(double))
    {
      bool converted = double.TryParse(value, out double d);

      if (!converted)
      {
        return (double.MinValue, col.DataType);
      }
      else
      {
        return (d, col.DataType);
      }
    }
    else if (col.DataType == typeof(long))
    {
      bool converted = long.TryParse(value, out long l);

      if (!converted)
      {
        return (long.MinValue, col.DataType);
      }
      else
      {
        return (l, col.DataType);
      }
    }
    else if (col.DataType.IsEnum)
    {
      bool converted = int.TryParse(value, out int i);

      if (!converted)
      {
        return (int.MinValue, col.DataType);
      }
      else
      {
        return (i, col.DataType);
      }
    }
    else if (col.DataType == typeof(byte))
    {
      bool converted = byte.TryParse(value, out byte i);

      if (!converted)
      {
        return (byte.MinValue, col.DataType);
      }
      else
      {
        return (i, col.DataType);
      }
    }
    else if (col.DataType == typeof(Guid))
    {
      bool converted = Guid.TryParse(value, out Guid i);

      if (!converted)
      {
        return (null, col.DataType);
      }
      else
      {
        return (i, col.DataType);
      }
    }

    return (null, null);
  }

  public static bool ContainsColumn(this DataRow row, string columnName)
  {
    return row.Table.Columns.Contains(columnName);
  }

  public static bool ContainsColumn(this DataRow row, string[] columnNames)
  {
    foreach (string column in columnNames)
    {
      if (row.Table.Columns.Contains(column))
        return true;
    }

    return false;
  }

  /// <summary>
  ///  Obtém a resposta de uma procedure
  /// </summary>
  /// <param name="command"></param>
  /// <param name="resultSetNames">Os nomes dos ResultSets</param>
  /// <param name="cancellationToken"></param>
  public static async Task<List<SqlResultSet>> QueryAsync(this DbCommand command, string[] resultSetNames,
    CancellationToken cancellationToken)
  {
    List<SqlResultSet> resultSets = new();

    command.CommandTimeout = 3600;

    using var reader = await command.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);

    int resultSetIndex = 0;

    do
    {
      var dataTable = new DataTable();

      dataTable.Load(reader); // Synchronous, but already buffered

      if (resultSetIndex < resultSetNames.Length)
      {
        dataTable.TableName = resultSetNames[resultSetIndex];
      }

      resultSets.Add(new SqlResultSet(dataTable));

      resultSetIndex++;
    } while (await reader.NextResultAsync(cancellationToken));

    return resultSets;
  }

  public static int GetTotalCount(this IEnumerable<SqlResultSet> data)
  {
    SqlResultSet? resultSet = data.GetResultSet(TotalCount).FirstOrDefault();

    if (resultSet?.Result == null || resultSet.Result.Rows.Count < 1)
    {
      return 0;
    }

    string value = resultSet.Result.Rows[0].GetColumnValue("total").value?.ToString() ?? "0";

    return int.TryParse(value, out int totalCount) ? totalCount : 0;
  }

  /// <summary>
  /// Retorna o valor como string
  /// </summary>
  /// <param name="value">O valor a ser convertido</param>
  /// <param name="defaultValue">O valor default de retorno caso value seja null</param>
  /// <param name="complementBefore">O complemento a ser adicionado na string caso não seja nula (não adicionada no defaultValue)</param>
  /// <param name="complementAfter"></param>
  public static string? AsString(this object? value, string? defaultValue = null, string? complementBefore = null, string? complementAfter = null)
  {
    StringBuilder returnValue = new();

    value = value?.ToString()?.Trim();

    if (value == DBNull.Value || string.IsNullOrEmpty(value?.ToString()))
    {
      return defaultValue;
    }
    else
    {
      if (!string.IsNullOrEmpty(complementBefore))
        returnValue.Append(complementBefore);

      returnValue.Append(((string)Convert.ChangeType(value, typeof(string))).Trim());

      if (!string.IsNullOrEmpty(complementAfter))
        returnValue.Append(complementAfter);
    }

    return returnValue.ToString();
  }
  
  public static int AsInteger(this object? value, int defaultValue = int.MinValue)
  {
    if (value == DBNull.Value || value == null)
      return defaultValue;

    return (int)Convert.ChangeType(value, typeof(int));
  }

  public static int? AsIntegerNullable(this object? value, int? defaultValue = null)
  {
    if (value == DBNull.Value || value == null || ((int)value) == int.MinValue)
      return defaultValue;

    return (int)Convert.ChangeType(value, typeof(int));
  }
  
  public static Guid AsGuid(this object? value, Guid defaultValue = new Guid())
  {
    if (value == DBNull.Value || value == null)
      return defaultValue;

    return (Guid)Convert.ChangeType(value, typeof(Guid));
  }

  public static Guid? AsGuidNullable(this object? value, Guid? defaultValue = null)
  {
    if (value == DBNull.Value || value == null)
      return defaultValue;

    return (Guid)Convert.ChangeType(value, typeof(Guid));
  }
  
  public static decimal AsDecimal(this object? value, decimal defaultValue = decimal.MinValue)
  {
    if (value == DBNull.Value || value == null)
      return defaultValue;

    return (decimal)Convert.ChangeType(value, typeof(decimal));
  }

  public static decimal? AsDecimalNullable(this object? value, decimal? defaultValue = null)
  {
    if (value == DBNull.Value || value == null || ((decimal)value) == decimal.MinValue)
      return defaultValue;

    return (decimal)Convert.ChangeType(value, typeof(decimal));
  }
  
  public static DateTime AsDateTimeLocal(this object? value, DateTime defaultValue = new DateTime())
  {
    if (value == DBNull.Value || value == null)
      return defaultValue;

    return ((DateTime)Convert.ChangeType(value, typeof(DateTime))).ToLocalTime();
  }

  public static DateTime AsDateTimeUniversal(this object? value, DateTime defaultValue = new DateTime())
  {
    if (value == DBNull.Value || value == null)
      return defaultValue;

    return ((DateTime)Convert.ChangeType(value, typeof(DateTime))).ToUniversalTime();
  }

  public static DateTime? AsDateTimeNullableLocal(this object? value, DateTime? defaultValue = null)
  {
    if (value == DBNull.Value || value == null || ((DateTime)value) == DateTime.MinValue)
      return defaultValue;

    return ((DateTime)Convert.ChangeType(value, typeof(DateTime))).ToLocalTime();
  }

  public static DateTime? AsDateTimeNullableUniversal(this object? value, DateTime? defaultValue = null)
  {
    if (value == DBNull.Value || value == null || ((DateTime)value) == DateTime.MinValue)
      return defaultValue;

    return ((DateTime)Convert.ChangeType(value, typeof(DateTime))).ToUniversalTime();
  }
  
  public static bool AsBoolean(this object? value, bool defaultValue = false)
  {
    if (value == DBNull.Value || value == null)
      return defaultValue;

    return (bool)Convert.ChangeType(value, typeof(bool));
  }

  public static bool? AsBooleanNullable(this object? value, bool? defaultValue = null)
  {
    if (value == DBNull.Value || value == null)
      return defaultValue;

    return (bool)Convert.ChangeType(value, typeof(bool));
  }
}

public class SqlResultSet(DataTable result)
{
  public bool? HasNext { get; set; } = false;

  public DataTable Result { get; set; } = result;
}