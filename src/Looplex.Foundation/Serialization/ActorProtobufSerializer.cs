using System;
using System.IO;

using Looplex.Foundation.Entities;

using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace Looplex.Foundation.Serialization.Protobuf;

public static class ActorProtobufSerializer
{

  public static byte[] Serialize<T>(this T actor) where T : Actor?
  {
    if (actor == null)
    {
      throw new ArgumentNullException(nameof(actor));
    }

    using MemoryStream memoryStream = new();
    Serializer.Serialize(memoryStream, actor);
    return memoryStream.ToArray();
  }

  public static T Deserialize<T>(this byte[] binary) where T : Actor
  {
    if (binary == null || binary.Length == 0)
    {
      throw new ArgumentException("Bite array cannot be null or empty.", nameof(binary));
    }

    using MemoryStream memoryStream = new(binary);
    return Serializer.Deserialize<T>(memoryStream);
  }
}