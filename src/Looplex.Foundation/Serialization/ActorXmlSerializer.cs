using System;
using System.IO;
using System.Xml.Serialization;

using Looplex.Foundation.Entities;

// ReSharper disable once CheckNamespace
namespace Looplex.Foundation.Serialization.Xml;

public static class ActorXmlSerializer
{
  public static string Serialize<T>(this T actor) where T : Actor?
  {
    if (actor == null)
      throw new ArgumentNullException(nameof(actor));

    XmlSerializer serializer = new(actor.GetType());
    using StringWriter writer = new();
    serializer.Serialize(writer, actor);
    return writer.ToString();
  }

  public static T Deserialize<T>(this string xml) where T : Actor
  {
    return (T)Deserialize(xml, typeof(T));
  }

  public static object Deserialize(this string xml, Type type)
  {
    if (!typeof(Actor).IsAssignableFrom(type)) // Must inherit from Actor
      throw new Exception($"Type {type.Name} must inherit from {nameof(Actor)}.");

    if (string.IsNullOrWhiteSpace(xml))
      throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));

    XmlSerializer serializer = new(type);
    using StringReader reader = new(xml);
    return serializer.Deserialize(reader);
  }
}