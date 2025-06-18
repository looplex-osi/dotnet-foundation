using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Looplex.Foundation.OAuth2.Entities
{
  public class OAuth2Service
  {
    private readonly FlowRouter _router;

    public OAuth2Service(FlowRouter router)
    {
      _router = router;
    }

    public async Task<string> Frontdoor(string json, string authentication, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentNullException(nameof(json));

      JObject jObj;
      try
      {
        jObj = JObject.Parse(json);
      }
      catch (JsonException ex)
      {
        throw new ArgumentException("Payload is not valid JSON.", nameof(json), ex);
      }

      var grantType = (string?)jObj["grant_type"];
      if (string.IsNullOrWhiteSpace(grantType))
        throw new ArgumentException("Missing grant_type.", nameof(json));

      return await _router.Route(grantType, json, authentication, cancellationToken)
                          .ConfigureAwait(false);
    }
  }
}
