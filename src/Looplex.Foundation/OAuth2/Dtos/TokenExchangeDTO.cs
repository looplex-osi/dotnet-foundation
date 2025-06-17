using Newtonsoft.Json;
using Looplex.Foundation.Entities;

namespace Looplex.Foundation.OAuth2.Dtos
{
    public class TokenExchangeDto : Actor
    {
        [JsonProperty("grant_type")]
        public string? GrantType { get; set; }

        [JsonProperty("subject_token")]
        public string? SubjectToken { get; set; }

        [JsonProperty("subject_token_type")]
        public string? SubjectTokenType { get; set; }
    }
}
