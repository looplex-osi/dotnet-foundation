using System;

namespace Looplex.Foundation.OAuth2.Entities;

/// <summary>
/// Expected failure of the token endpoint (RFC 6749 §5.2, RFC 8693 §2.2.2). The host answers it with a JSON
/// <c>error</c>/<c>error_description</c> body instead of a 500. <see cref="Exception.Message"/> becomes the
/// <c>error_description</c>: it must be a fixed text and never echo request input or internal details.
/// </summary>
public class OAuth2Exception : Exception
{
  public const string InvalidRequest = "invalid_request";
  public const string InvalidClient = "invalid_client";
  public const string UnsupportedGrantType = "unsupported_grant_type";
  public const string InvalidTarget = "invalid_target";

  public OAuth2Exception(string error, string description) : base(description)
  {
    Error = error;
  }

  /// <summary>The OAuth2 <c>error</c> code.</summary>
  public string Error { get; }
}
