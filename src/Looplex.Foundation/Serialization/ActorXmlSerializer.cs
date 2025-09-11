using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

using Looplex.Foundation.Entities;

namespace Looplex.Foundation.Serialization
{
  public static class ActorXmlSerializer
  {
    public static string Serialize(Actor actor)
    {
      if (actor == null) throw new ArgumentNullException(nameof(actor));
      var serializer = new XmlSerializer(actor.GetType());
      using var writer = new StringWriter();
      serializer.Serialize(writer, actor);
      return writer.ToString();
    }

    public static T Deserialize<T>(string xml) where T : Actor
    {
      if (string.IsNullOrWhiteSpace(xml))
        throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
      var serializer = new XmlSerializer(typeof(T));
      using var reader = new StringReader(xml);
      var result = (T)serializer.Deserialize(reader);
      if (result == null)
        throw new InvalidOperationException($"Failed to deserialize XML to {typeof(T).Name}");
      return result;
    }
  }
}