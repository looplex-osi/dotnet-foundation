using System;

using Looplex.Foundation.Entities;

using PropertyChanged;

using ProtoBuf;

namespace Looplex.SCIMv2.Entities;

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public abstract class Resource : Actor, IResource
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public Resource() : base() { }

  #endregion

  [ProtoMember(1)] public string Id { get; set; } = string.Empty;

  [ProtoMember(2)] public string? ExternalId { get; set; }

  [ProtoMember(3)] public ResourceMeta Meta { get; set; } = new();

  [ProtoMember(4)] public string[] Schemas { get; set; } = Array.Empty<string>();
}

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public class ResourceMeta
{
  [ProtoMember(1)] public string ResourceType { get; set; } = string.Empty;

  [ProtoMember(2)] public DateTime Created { get; set; }

  [ProtoMember(3)] public DateTime LastModified { get; set; }

  [ProtoMember(4)] public string Location { get; set; } = string.Empty;

  [ProtoMember(5)] public string Version { get; set; } = string.Empty;
}
