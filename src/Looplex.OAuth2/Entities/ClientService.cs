using System;
using System.Text.Json.Serialization;

using PropertyChanged;

namespace Looplex.OAuth2.Entities;

/// <summary>
/// Represents an OAuth2 client service entity with authentication credentials and temporal constraints.
/// This class encapsulates client identification, authentication secrets, and access control parameters
/// following OAuth2 specification standards for client registration and management.
/// </summary>
[AddINotifyPropertyChangedInterface]
public class ClientService
{
  #region Reflectivity

  /// <summary>
  /// Initializes a new instance of the ClientService class.
  /// Required for property change notification and serialization support.
  /// </summary>
  // ReSharper disable once EmptyConstructor
  public ClientService() : base() { }

  #endregion

  /// <summary>
  /// Gets or sets the human-readable name of the OAuth2 client application.
  /// This value is used for display purposes and client identification in administrative interfaces.
  /// </summary>
  public string? ClientName { get; set; }

  /// <summary>
  /// Gets or sets the username associated with this client service.
  /// Used for user-specific client service management and access control.
  /// </summary>
  public string? UserName { get; set; }

  /// <summary>
  /// Gets or sets the unique identifier for this client service.
  /// Automatically generated as a GUID string for unique client identification.
  /// </summary>
  public string Id { get; set; } = Guid.NewGuid().ToString();

  /// <summary>
  /// Gets or sets the client secret used for OAuth2 client authentication.
  /// This secret is used in the client credentials flow and must be kept confidential.
  /// Serialized as "client_secret" in JSON responses.
  /// </summary>
  [JsonPropertyName("client_secret")] public string? ClientSecret { get; set; }
  
  /// <summary>
  /// Gets or sets the digest hash of the client secret for secure storage and verification.
  /// This property is excluded from JSON serialization for security purposes.
  /// </summary>
  [JsonIgnore] public string? Digest { get; set; }

  /// <summary>
  /// Gets or sets the user ID associated with this client service.
  /// Used for user-specific client management and access control.
  /// This property is excluded from JSON serialization.
  /// </summary>
  [JsonIgnore] public int? UserId { get; set; }

  /// <summary>
  /// Gets or sets the expiration time for this client service.
  /// After this time, the client service will no longer be valid for authentication.
  /// </summary>
  public DateTimeOffset ExpirationTime { get; set; }

  /// <summary>
  /// Gets or sets the "not before" time for this client service.
  /// The client service will not be valid for authentication before this time.
  /// </summary>
  public DateTimeOffset NotBefore { get; set; }
}
