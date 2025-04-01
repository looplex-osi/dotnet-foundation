using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Http;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.SCIMv2.Entities;

public class AttributesProcessor
{
  public IList<JObject> ProcessAttributes(HttpContext context, List<JObject> records)
  {
    var query = context.Request.Query;

    var attrs = query.ContainsKey("attributes")
      ? query["attributes"].ToString().Split([','], StringSplitOptions.RemoveEmptyEntries)
      : [];

    var xattrs = query.ContainsKey("excludedAttributes")
      ? query["excludedAttributes"].ToString().Split([','], StringSplitOptions.RemoveEmptyEntries)
      : [];

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
}