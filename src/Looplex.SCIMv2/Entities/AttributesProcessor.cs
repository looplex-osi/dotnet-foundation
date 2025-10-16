using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Looplex.SCIMv2.Entities;

public static class AttributesProcessor
{
  // Helper to navigate JSON paths
  private static JsonNode? GetJsonValue(JsonObject obj, string path)
  {
    var parts = path.Split('.');
    JsonNode? current = obj;
    
    foreach (var part in parts)
    {
      if (current is JsonObject jsonObj && jsonObj.TryGetPropertyValue(part, out var value))
      {
        current = value;
      }
      else if (current is JsonArray jsonArray && int.TryParse(part, out var index) && index < jsonArray.Count)
      {
        current = jsonArray[index];
      }
      else
      {
        return null;
      }
    }
    
    return current;
  }
  
  // Helper to set values in JSON paths
  private static void SetJsonValue(JsonObject obj, string path, JsonNode? value)
  {
    var parts = path.Split('.');
    JsonNode? current = obj;
    
    for (int i = 0; i < parts.Length - 1; i++)
    {
      var part = parts[i];
      
      if (current is JsonObject jsonObj)
      {
        if (!jsonObj.TryGetPropertyValue(part, out var next))
        {
          next = new JsonObject();
          jsonObj[part] = next;
        }
        current = next;
      }
    }
    
    if (current is JsonObject finalObj)
    {
      finalObj[parts[^1]] = value;
    }
  }
  
  // Helper to delete values in JSON paths
  private static void DeleteJsonValue(JsonObject obj, string path)
  {
    var parts = path.Split('.');
    JsonNode? current = obj;
    
    for (int i = 0; i < parts.Length - 1; i++)
    {
      var part = parts[i];
      
      if (current is JsonObject jsonObj && jsonObj.TryGetPropertyValue(part, out var next))
      {
        current = next;
      }
      else
      {
        return; // Path does not exist
      }
    }
    
    if (current is JsonObject finalObj)
    {
      finalObj.Remove(parts[^1]);
    }
  }

  public static IEnumerable<JsonObject> ProcessAttributes(this IEnumerable<JsonObject> records, HttpContext context)
  {
    var query = context.Request.Query;
    var attrs = new string[0];
    if(context.Request.Query.ContainsKey("attributes"))
    {
      attrs = context.Request.Query["attributes"].ToString().Split([','], StringSplitOptions.RemoveEmptyEntries);
    }

    var xattrs = query.ContainsKey("excludedAttributes")
      ? query["excludedAttributes"].ToString().Split([','], StringSplitOptions.RemoveEmptyEntries)
      : [];

    if (attrs.Length > 0)
    {
      records = records
        .Select(record =>
        {
          var newObj = new JsonObject();
          foreach (var attr in attrs)
          {
            var value = GetJsonValue(record, attr);
            if (value != null)
            {
              // Create a deep copy using JSON serialization to avoid parent issues
              var clonedValue = JsonNode.Parse(JsonSerializer.Serialize(value));
              SetJsonValue(newObj, attr, clonedValue);
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
          DeleteJsonValue(record, xattr);
        }
      }
    }

    return records;
  }
}
