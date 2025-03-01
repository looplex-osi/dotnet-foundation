using Looplex.Foundation.Entities;
using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.OAuth2.Dtos;

public class ClientCredentialsDto : Actor
{
    #region Reflectivity
    // ReSharper disable once PublicConstructorInAbstractClass
    public ClientCredentialsDto() : base() { }
    #endregion
    
    [JsonProperty("grant_type")]
    public required string GrantType { get; init; }
    
    [JsonProperty("subject_token")]
    public string? SubjectToken { get; init; }
    
    [JsonProperty("subject_token_type")]
    public string? SubjectTokenType { get; init; }
}