using System;
using System.IO;

using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization;

using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace Looplex.SCIMv2.Serialization;

public static class ActorProtobufSerializer
{
  public static byte[] Serialize<T>(this T actor) where T : Actor?
  {
    if (actor == null)
    {
      throw new ArgumentNullException(nameof(actor));
    }

    return Foundation.Serialization.ProtobufSerializer.SerializeActor(actor);
  }

  public static T? Deserialize<T>(this byte[] binary) where T : Actor
  {
    return Foundation.Serialization.ProtobufSerializer.DeserializeActor<T>(binary);
  }

  public static object Deserialize(this byte[] binary, Type type)
  {
    if (!typeof(Actor).IsAssignableFrom(type)) // Must inherit from Actor
      throw new Exception($"Type {type.Name} must inherit from {nameof(Actor)}.");

    if (binary == null || binary.Length == 0)
      throw new ArgumentException("Bite array cannot be null or empty.", nameof(binary));

    return Foundation.Serialization.ProtobufSerializer.DeserializeActor(binary, type);
  }
}
