using System;
using Looplex.Foundation.SCIMv2.Entities;
using Newtonsoft.Json;

namespace Looplex.Foundation.OAuth2.Entities;

public class ClientCredential : Resource
{
    public virtual string? ClientName { get; set; }
    public virtual Guid? ClientId { get; set; }

    [JsonIgnore]
    public string? Digest { get; set; }
    
    [JsonIgnore]
    public int? UserId { get; set; }
    
    public virtual DateTimeOffset ExpirationTime { get; set; }
    public virtual DateTimeOffset NotBefore { get; set; }
}