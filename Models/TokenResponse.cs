using System;
using System.Text.Json.Serialization;

namespace OAuth2._0.Models;

/// <summary>
/// Represents an OAuth 2.0 token response according to RFC 6749
/// </summary>
public class TokenResponse
{
	/// <summary>
	/// The access token issued by the authorization server
	/// </summary>
	[JsonPropertyName("access_token")]
	public string AccessToken { get; set; } = string.Empty;

	/// <summary>
	/// The type of the token issued (typically "Bearer")
	/// </summary>
	[JsonPropertyName("token_type")]
	public string TokenType { get; set; } = "Bearer";

	/// <summary>
	/// The lifetime in seconds of the access token
	/// </summary>
	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }

	/// <summary>
	/// The refresh token, which can be used to obtain new access tokens
	/// </summary>
	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; set; }

	/// <summary>
	/// The scope of the access token
	/// </summary>
	[JsonPropertyName("scope")]
	public string? Scope { get; set; }

	/// <summary>
	/// Additional custom parameters from the token response
	/// </summary>
	[JsonExtensionData]
	public Dictionary<string, object>? AdditionalParameters { get; set; }

	/// <summary>
	/// Calculated expiration time in UTC
	/// </summary>
	[JsonIgnore]
	public DateTime ExpiresAtUtc { get; set; }

	/// <summary>
	/// Checks if the token is expired (with 1 minute buffer)
	/// </summary>
	[JsonIgnore]
	public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow.AddMinutes(1);
}