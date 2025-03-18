using System;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation;

public static class JsonUtils
{
  public static void Traverse(JToken token, Action<JToken> visitor)
  {
    visitor(token);

    foreach (var child in token.Children())
    {
      Traverse(child, visitor);
    }
  }
}