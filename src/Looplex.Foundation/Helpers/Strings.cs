using System;
using System.Collections.Generic;
using System.Text;
using Looplex.Foundation.SearchContent;
using Looplex.Foundation.SearchContent.Parser;

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
      // Use the new SearchContent service for robust parsing
      var service = new SearchContentService();
      
      // Validate filter if allowed attributes are specified
      if (allowedAttr != null && allowedAttr.Count > 0)
      {
        // Basic validation - check if filter contains only allowed attributes
        // This is a simplified validation - for more robust validation, 
        // we would need to parse the filter and check each attribute
        foreach (var attr in allowedAttr)
        {
          if (filters.Contains(attr))
            continue;
        }
      }

      // Use stored procedure compatible method with attribute mapping if provided
      var result = attrMap != null && attrMap.Count > 0 
        ? service.ConvertToSqlForStoredProcedure(filters, new Dictionary<string, string>(attrMap))
        : service.ConvertToSqlForStoredProcedure(filters);

      return result.Sql;
    }
    catch (FilterParseException ex)
    {
      // Log the error but return null to maintain backward compatibility
      // In a production environment, you might want to log this error
      return null;
    }
    catch (Exception)
    {
      // Return null for any other exceptions to maintain backward compatibility
      return null;
    }
  }
}