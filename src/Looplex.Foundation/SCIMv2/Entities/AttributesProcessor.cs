using System;
using System.Collections.Generic;
using System.Linq;

using Looplex.Foundation.Helpers;

using Microsoft.AspNetCore.Http;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.SCIMv2.Entities;

/// <summary>
/// SCIM v2 attribute projection (RFC 7644 §3.4.2.5): <c>attributes</c> keeps only the listed paths,
/// <c>excludedAttributes</c> removes paths. Applied after the records are already in their response casing.
/// </summary>
public static class AttributesProcessor
{
  /// <summary>Reads <c>attributes</c> and <c>excludedAttributes</c> from the request query string.</summary>
  public static IEnumerable<JObject> ProcessAttributes(this IEnumerable<JObject> records, HttpContext context)
  {
    var query = context.Request.Query;

    string? attributes = query.ContainsKey("attributes") ? query["attributes"].ToString() : null;
    string? excludedAttributes = query.ContainsKey("excludedAttributes") ? query["excludedAttributes"].ToString() : null;

    return records.ProcessAttributes(attributes, excludedAttributes);
  }

  /// <summary>
  /// Transport-agnostic overload for drivers that are not HTTP (MCP tools, workers).
  /// Both parameters are comma-separated paths; null or empty means "no projection".
  /// </summary>
  public static IEnumerable<JObject> ProcessAttributes(this IEnumerable<JObject> records, string? attributes, string? excludedAttributes)
  {
    string[] attrs = Split(attributes);
    string[] xattrs = Split(excludedAttributes);

    if (attrs.Length > 0)
    {
      records = records
        .Select(record =>
        {
          var newObj = new JObject();
          foreach (var attr in attrs)
          {
            var value = JsonHelper._get(record, attr);
            if (value != null)
            {
              JsonHelper._set(newObj, attr, value.DeepClone());
            }
          }

          return newObj;
        })
        .ToList();
    }

    if (xattrs.Length > 0)
    {
      foreach (var record in records)
      {
        foreach (var xattr in xattrs)
        {
          JsonHelper._delete(record, xattr);
        }
      }
    }

    return records;
  }

  private static string[] Split(string? csv) =>
    string.IsNullOrWhiteSpace(csv)
      ? []
      : csv.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
}
