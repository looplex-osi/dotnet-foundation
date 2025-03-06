using System.Collections.Generic;

using PropertyChanged;

using ProtoBuf;

namespace Looplex.Foundation.SCIMv2.Entities;

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public class User : Resource
{
  #region Reflectivity

  #endregion

  [ProtoMember(1)] public string UserName { get; set; }

  [ProtoMember(2)] public ScimName Name { get; set; }

  [ProtoMember(3)] public string DisplayName { get; set; }

  [ProtoMember(4)] public string NickName { get; set; }

  [ProtoMember(5)] public string ProfileUrl { get; set; }

  [ProtoMember(6)] public string Title { get; set; }

  [ProtoMember(7)] public string UserType { get; set; }

  [ProtoMember(8)] public string PreferredLanguage { get; set; }

  [ProtoMember(9)] public string Locale { get; set; }

  [ProtoMember(10)] public string Timezone { get; set; }

  [ProtoMember(11)] public bool Active { get; set; }

  // multi-valued attributes
  [ProtoMember(12)] public List<ScimEmail> Emails { get; set; } = new();

  [ProtoMember(13)] public List<ScimPhoneNumber> PhoneNumbers { get; set; } = new();

  [ProtoMember(14)] public List<ScimAddress> Addresses { get; set; } = new();

  // groups to which this user belongs
  [ProtoMember(15)] public List<ScimGroupRef> Groups { get; set; } = new();
}

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public class ScimName
{
  [ProtoMember(1)] public string Formatted { get; set; }

  [ProtoMember(2)] public string FamilyName { get; set; }

  [ProtoMember(3)] public string GivenName { get; set; }

  [ProtoMember(4)] public string MiddleName { get; set; }

  [ProtoMember(5)] public string HonorificPrefix { get; set; }

  [ProtoMember(6)] public string HonorificSuffix { get; set; }
}

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public class ScimEmail
{
  [ProtoMember(1)] public string Value { get; set; }

  [ProtoMember(2)] public string Type { get; set; }

  [ProtoMember(3)] public bool Primary { get; set; }
}

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public class ScimPhoneNumber
{
  [ProtoMember(1)] public string Value { get; set; }

  [ProtoMember(2)] public string Type { get; set; }
}

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public class ScimAddress
{
  [ProtoMember(1)] public string Formatted { get; set; }

  [ProtoMember(2)] public string StreetAddress { get; set; }

  [ProtoMember(3)] public string Locality { get; set; }

  [ProtoMember(4)] public string Region { get; set; }

  [ProtoMember(5)] public string PostalCode { get; set; }

  [ProtoMember(6)] public string Country { get; set; }

  [ProtoMember(7)] public string Type { get; set; }

  [ProtoMember(8)] public bool Primary { get; set; }
}

[ProtoContract]
[AddINotifyPropertyChangedInterface]
public class ScimGroupRef
{
  [ProtoMember(1)] public string Value { get; set; }

  [ProtoMember(2)] public string Display { get; set; }

  [ProtoMember(3)] public string Type { get; set; }

  [ProtoMember(4)] public string Ref { get; set; }
}