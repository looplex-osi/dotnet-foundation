using Newtonsoft.Json;
using Looplex.Foundation.Entities;

namespace Looplex.Foundation.OAuth2.Dtos
{
    public class TokenExchangeDto : Actor
    {
        [JsonProperty("grant_type", Required = Required.Always)]
        public string GrantType { get; set; } = default!;

        [JsonProperty("subject_token", Required = Required.Always)]
        public string SubjectToken { get; set; } = default!;

        [JsonProperty("subject_token_type", Required = Required.Always)]
        public string SubjectTokenType { get; set; } = default!;
    }
}
