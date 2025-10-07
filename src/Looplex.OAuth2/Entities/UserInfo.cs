using System.Text.Json.Serialization;

using Looplex.Foundation.Entities;

namespace Looplex.OAuth2.Entities;

/// <summary>
/// Represents user information retrieved from OAuth2/OIDC userinfo endpoint.
/// This class encapsulates standard user profile data following OpenID Connect specification
/// and provides structured access to user identity information for authentication and authorization.
/// </summary>
public sealed class UserInfo : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  /// <summary>
  /// Gets or sets the subject identifier for the user.
  /// This is the unique identifier for the user within the identity provider system.
  /// Serialized as "sub" in JSON responses following OIDC specification.
  /// </summary>
  [JsonPropertyName("sub")] public string Sub { get; set; }

  /// <summary>
  /// Gets or sets the full display name of the user.
  /// This is the complete name as it should be displayed to other users.
  /// Serialized as "name" in JSON responses.
  /// </summary>
  [JsonPropertyName("name")] public string Name { get; set; }

  /// <summary>
  /// Gets or sets the family name (surname) of the user.
  /// This is the user's last name or family name.
  /// Serialized as "family_name" in JSON responses.
  /// </summary>
  [JsonPropertyName("family_name")] public string FamilyName { get; set; }

  /// <summary>
  /// Gets or sets the given name (first name) of the user.
  /// This is the user's first name or given name.
  /// Serialized as "given_name" in JSON responses.
  /// </summary>
  [JsonPropertyName("given_name")] public string GivenName { get; set; }

  /// <summary>
  /// Gets or sets the URL of the user's profile picture.
  /// This URL points to an image that represents the user.
  /// Serialized as "picture" in JSON responses.
  /// </summary>
  [JsonPropertyName("picture")] public string Picture { get; set; }

  /// <summary>
  /// Gets or sets the email address of the user.
  /// This is the primary email address associated with the user's account.
  /// Serialized as "email" in JSON responses.
  /// </summary>
  [JsonPropertyName("email")] public string Email { get; set; }
}
