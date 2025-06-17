using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
      var jObj = JObject.Parse(json);
      var grantType = jObj["grant_type"]?.ToString();

      if (string.IsNullOrWhiteSpace(grantType))
        throw new ArgumentNullException("grant_type", "Missing grant_type.");

      return await _router.Route(grantType, json, authentication, cancellationToken);
    }
  }
}
