using System.ComponentModel;
using System.Xml.Serialization;

using Looplex.Foundation.Entities;

namespace Looplex.Foundation.Serialization
{
  public static class ActorXmlSerializer
  {
    public static string Serialize(Actor actor)
    {
      ArgumentNullException.ThrowIfNull(actor);
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
      return (T)serializer.Deserialize(reader);
    }
  }
}