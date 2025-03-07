using System.ComponentModel;
using System.Text.Json;

using Looplex.Foundation.Entities;

namespace Looplex.Foundation.Serialization
{
  public static class ActorJsonSerializer
  {
    public static string Serialize(Actor actor)
    {
      ArgumentNullException.ThrowIfNull(actor);
      var options = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };
      string json = JsonSerializer.Serialize(actor, actor.GetType(), options);
      return json;
    }

    public static T Deserialize<T>(string json) where T : Actor
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      var options = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };
      return JsonSerializer.Deserialize<T>(json, options);
    }
  }
}