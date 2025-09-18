using System;
using System.Collections.Generic;
using System.Text;

using Looplex.Foundation.SearchContent;
using Looplex.Foundation.SearchContent.SqlGenerator;

namespace Looplex.Foundation.Helpers;

public static class Strings
{
  public static string Base64Decode(string base64EncodedData)
  {
    byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
    return Encoding.UTF8.GetString(base64EncodedBytes);
  }

  /// <summary>
  /// Converts a SCIMv2 defined filters query param into a SQL predicate 
  /// 
  /// Backward compatibility behavior:
  /// - When attrMap is provided: Generates inline SQL for stored procedures (UseParameters = false)
  /// - When attrMap is null: Generates parameterized SQL for direct execution (UseParameters = true)
  /// </summary>
  /// <param name="filters"></param>
  /// <param name="attrMap"></param>
  /// <param name="allowedAttr"></param>
  /// <returns></returns>
  public static string? ToSqlPredicate(this string? filters, IDictionary<string, string>? attrMap = null,
    HashSet<string>? allowedAttr = null)
  {
    if (string.IsNullOrEmpty(filters))
      return null;

    try
    {
      var service = new SearchContentService();
      
      // Detect stored procedure usage: when attrMap is provided, use inline SQL
      var options = new SqlGenerationOptions
      {
        FieldMapping = attrMap != null ? new Dictionary<string, string>(attrMap) : new Dictionary<string, string>(),
        UseParameters = attrMap == null, // Use parameters only when no attrMap (direct execution)
        EscapeStrings = true
      };
      
      var result = service.ConvertToSql(filters, options);
      return result.Sql;
    }
    catch
    {
      // Return null on parsing errors for backward compatibility
      return null;
    }
  }
}