using System;
using System.Collections.Generic;
using System.Text;

using Antlr4.Runtime;

using Looplex.Foundation.SCIMv2.Antlr;
using Looplex.Foundation.SCIMv2.Entities;

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
    string? result = null;

    if (!string.IsNullOrEmpty(filters))
    {
      var inputStream = new AntlrInputStream(filters);
      var lexer = new ScimFilterLexer(inputStream);
      var tokens = new CommonTokenStream(lexer);
      var parser = new ScimFilterParser(tokens);
      var tree = parser.parse();
      var visitor = new SCIMv2ToSQLVisitor { AttributeMapper = attrMap, AllowedAttributes = allowedAttr};
      result = visitor.Visit(tree);
    }

    return result;
  }
}