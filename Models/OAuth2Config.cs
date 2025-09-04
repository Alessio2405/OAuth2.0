using System.Collections.Generic;

namespace OAuth2._0.Models;

/// <summary>
/// Configuration for OAuth 2.0 client
/// </summary>
public class OAuth2Config
{
	/// <summary>
	/// The client identifier issued to the client during registration
	/// </summary>
	public required string ClientId { get; set; }

	/// <summary>
	/// The client secret (optional for public clients)
	/// </summary>
	public string? ClientSecret { get; set; }

	/// <summary>
	/// The authorization endpoint URL
	/// </summary>
	public required string AuthorizationUrl { get; set; }

	/// <summary>
	/// The token endpoint URL
	/// </summary>
	public required string TokenUrl { get; set; }

	/// <summary>
	/// The client's redirect URI
	/// </summary>
	public string RedirectUri { get; set; } = "http://localhost:8080/callback";

	/// <summary>
	/// Space-separated list of scopes
	/// </summary>
	public string? Scopes { get; set; }

	/// <summary>
	/// Enable PKCE (Proof Key for Code Exchange) for enhanced security
	/// </summary>
	public bool UsePKCE { get; set; } = true;

	/// <summary>
	/// Optional revocation endpoint URL
	/// </summary>
	public string? RevocationUrl { get; set; }

	/// <summary>
	/// Additional parameters to include in the authorization request
	/// </summary>
	public Dictionary<string, string>? AdditionalAuthParams { get; set; }

	/// <summary>
	/// Additional parameters to include in the token request
	/// </summary>
	public Dictionary<string, string>? AdditionalTokenParams { get; set; }

	/// <summary>
	/// Request timeout in seconds (default: 30)
	/// </summary>
	public int TimeoutSeconds { get; set; } = 30;
}