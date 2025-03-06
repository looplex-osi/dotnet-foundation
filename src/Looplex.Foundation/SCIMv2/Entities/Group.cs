using System.Collections.Generic;

using PropertyChanged;

using ProtoBuf;

namespace Looplex.Foundation.SCIMv2.Entities;

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public class Group : Resource
{
  #region Reflectivity
  public Group() : base() { }
  #endregion

  [ProtoMember(1)] public string DisplayName { get; set; }

  [ProtoMember(2)] public List<ScimMemberRef> Members { get; set; } = new();
}

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public class ScimMemberRef
{
  [ProtoMember(1)] public string Value { get; set; }

  [ProtoMember(2)] public string Display { get; set; }

  [ProtoMember(3)] public string Type { get; set; }

  [ProtoMember(4)] public string Ref { get; set; }
}