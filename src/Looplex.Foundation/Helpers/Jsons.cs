using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.Helpers;

public static class JsonHelper
{
  public static void Traverse(JToken token, Action<JToken> visitor)
  {
    visitor(token);

    foreach (var child in token.Children())
    {
      Traverse(child, visitor);
    }
  }

  /// <summary>
  /// Converts a string path into a list of tokens.
  /// Supports dot notation, bracket notation with numbers, and quoted strings.
  /// </summary>
  public static List<PathToken> ItFromPath(string path)
  {
    if (path == "")
      return new List<PathToken> { new PathToken { Key = "", IsArrayIndex = false } };

    var tokens = new List<PathToken>();
    // Regex: captures property names in dot notation,
    // numeric indices inside brackets, and quoted property names.
    var re = new Regex(@"([^.[\]]+)|\[(\d+)\]|(?:\[""([^""]+)""\])|(?:\['([^']+)'\])");
    var matches = re.Matches(path);
    foreach (Match match in matches)
    {
      if (match.Groups[1].Success)
      {
        // Property name in dot notation.
        tokens.Add(new PathToken { Key = match.Groups[1].Value, IsArrayIndex = false });
      }
      else if (match.Groups[2].Success)
      {
        // Numeric index inside brackets.
        tokens.Add(new PathToken { Key = match.Groups[2].Value, IsArrayIndex = true });
      }
      else if (match.Groups[3].Success)
      {
        // Double-quoted property inside brackets.
        tokens.Add(new PathToken { Key = match.Groups[3].Value, IsArrayIndex = false });
      }
      else if (match.Groups[4].Success)
      {
        // Single-quoted property inside brackets.
        tokens.Add(new PathToken { Key = match.Groups[4].Value, IsArrayIndex = false });
      }
    }

    return tokens;
  }

  /// <summary>
  /// Retrieves the value at the specified path of the object.
  /// If the value doesn't exist, returns the provided defaultValue.
  /// </summary>
  public static JToken? _get(JToken obj, string path, JToken defaultValue = null)
  {
    if (obj == null || (obj.Type != JTokenType.Object && obj.Type != JTokenType.Array))
      throw new ArgumentException("obj needs to be an object or array");

    JToken o = obj;
    var tokens = ItFromPath(path);
    for (int i = 0; i < tokens.Count; i++)
    {
      var token = tokens[i];
      string key = token.Key;
      if (o == null)
        return defaultValue;

      bool isLast = (i == tokens.Count - 1);
      if (o.Type == JTokenType.Object)
      {
        var jObj = (JObject)o;
        if (!jObj.ContainsKey(key))
          return defaultValue;
        if (isLast)
          return jObj[key] ?? defaultValue;
        o = jObj[key];
      }
      else if (o.Type == JTokenType.Array)
      {
        // When working with arrays, key should represent an index.
        if (!int.TryParse(key, out int index))
          return defaultValue;
        var jArr = (JArray)o;
        if (index < 0 || index >= jArr.Count)
          return defaultValue;
        if (isLast)
          return jArr[index] ?? defaultValue;
        o = jArr[index];
      }
      else
      {
        return defaultValue;
      }
    }

    return o;
  }

  /// <summary>
  /// Sets the value at the specified path of the object.
  /// Creates intermediate objects/arrays if needed.
  /// Arrays are created for numeric indices; otherwise, objects are used.
  /// </summary>
  public static JToken _set(JToken obj, string path, JToken value)
  {
    if (obj == null || (obj.Type != JTokenType.Object && obj.Type != JTokenType.Array))
      throw new ArgumentException("obj needs to be an object or array");

    JToken o = obj;
    var tokens = ItFromPath(path);
    for (int i = 0; i < tokens.Count; i++)
    {
      var token = tokens[i];
      string key = token.Key;
      bool isLast = (i == tokens.Count - 1);

      if (o.Type == JTokenType.Object)
      {
        var jObj = (JObject)o;
        if (isLast)
        {
          jObj[key] = value;
        }
        else
        {
          if (!jObj.ContainsKey(key) ||
              (jObj[key].Type != JTokenType.Object && jObj[key].Type != JTokenType.Array))
          {
            var nextToken = tokens[i + 1];
            jObj[key] = nextToken.IsArrayIndex ? (JToken)new JArray() : new JObject();
          }

          o = jObj[key];
        }
      }
      else if (o.Type == JTokenType.Array)
      {
        if (!int.TryParse(key, out int index))
          throw new Exception("Invalid array index in path.");

        var jArr = (JArray)o;
        // Expand the array if necessary.
        while (jArr.Count <= index)
        {
          jArr.Add(JValue.CreateNull());
        }

        if (isLast)
        {
          jArr[index] = value;
        }
        else
        {
          if (jArr[index] == null ||
              (jArr[index].Type != JTokenType.Object && jArr[index].Type != JTokenType.Array))
          {
            var nextToken = tokens[i + 1];
            jArr[index] = nextToken.IsArrayIndex ? (JToken)new JArray() : new JObject();
          }

          o = jArr[index];
        }
      }
      else
      {
        throw new Exception("Encountered a non-object/non-array in path.");
      }
    }

    return obj;
  }

  /// <summary>
  /// Deletes the property at the specified path of the object.
  /// If the property does not exist, nothing happens.
  /// Supports both dot and bracket notation.
  /// </summary>
  public static bool _delete(JToken obj, string path)
  {
    if (obj == null || (obj.Type != JTokenType.Object && obj.Type != JTokenType.Array))
      throw new ArgumentException("obj needs to be an object or array");
    if (path == null)
      throw new ArgumentException("path needs to be a string");

    var tokens = ItFromPath(path);
    JToken o = obj;
    for (int i = 0; i < tokens.Count; i++)
    {
      var token = tokens[i];
      string key = token.Key;
      bool isArrayIndex = token.IsArrayIndex;
      bool isLast = (i == tokens.Count - 1);

      if (isLast)
      {
        if (o.Type == JTokenType.Array && isArrayIndex)
        {
          if (int.TryParse(key, out int index))
          {
            var jArr = (JArray)o;
            if (index >= 0 && index < jArr.Count)
            {
              jArr.RemoveAt(index);
            }
          }
        }
        else if (o.Type == JTokenType.Object)
        {
          var jObj = (JObject)o;
          jObj.Remove(key);
        }

        return true;
      }

      // Traverse deeper into the structure.
      if (o.Type == JTokenType.Object)
      {
        var jObj = (JObject)o;
        if (!jObj.ContainsKey(key))
          return true;
        o = jObj[key];
      }
      else if (o.Type == JTokenType.Array)
      {
        if (!int.TryParse(key, out int index))
          return true;
        var jArr = (JArray)o;
        if (index < 0 || index >= jArr.Count)
          return true;
        o = jArr[index];
      }
      else
      {
        return true;
      }
    }

    return true;
  }
}

public class PathToken
{
  public string Key { get; set; }
  public bool IsArrayIndex { get; set; }
}