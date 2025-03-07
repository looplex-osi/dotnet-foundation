using System;
using System.IO;
using System.Xml.Serialization;

using Looplex.Foundation.Entities;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using ProtoBuf;

namespace Looplex.Foundation.Serialization;

public static class ActorSerializationStrategies
{
  public static string JsonSerialize<T>(this T actor) where T : Actor
  {
    if (actor == null)
    {
      throw new ArgumentNullException(nameof(actor));
    }

    JsonSerializerSettings settings = new()
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented
    };
    string json = JsonConvert.SerializeObject(actor, actor.GetType(), settings);
    return json;
  }

  public static T JsonDeserialize<T>(this string json) where T : Actor
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
    }

    JsonSerializerSettings settings = new()
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
    return JsonConvert.DeserializeObject<T>(json, settings);
  }

  public static string XmlSerialize<T>(this T actor) where T : Actor
  {
    if (actor == null)
    {
      throw new ArgumentNullException(nameof(actor));
    }

    XmlSerializer serializer = new(actor.GetType());
    using (StringWriter writer = new())
    {
      serializer.Serialize(writer, actor);
      return writer.ToString();
    }
  }

  public static T XmlDeserialize<T>(this string xml) where T : Actor
  {
    if (string.IsNullOrWhiteSpace(xml))
    {
      throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
    }

    XmlSerializer serializer = new(typeof(T));
    using (StringReader reader = new(xml))
    {
      return (T)serializer.Deserialize(reader);
    }
  }

  public static byte[] ProtobufSerialize<T>(this T actor) where T : Actor
  {
    if (actor == null)
    {
      throw new ArgumentNullException(nameof(actor));
    }

    using (MemoryStream memoryStream = new())
    {
      Serializer.Serialize(memoryStream, actor);
      return memoryStream.ToArray();
    }
  }

  public static T ProtobufDeserialize<T>(this byte[] binary) where T : Actor
  {
    if (binary == null || binary.Length == 0)
    {
      throw new ArgumentException("Bite array cannot be null or empty.", nameof(binary));
    }

    using (MemoryStream memoryStream = new(binary))
    {
      return Serializer.Deserialize<T>(memoryStream);
    }
  }
}