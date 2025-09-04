using System;
using System.Security.Cryptography;
using System.Text;

namespace OAuth2._0.Utils;

/// <summary>
/// Helper class for PKCE (Proof Key for Code Exchange) implementation
/// RFC 7636: https://tools.ietf.org/html/rfc7636
/// </summary>
internal static class PKCEHelper
{
	private const int MinVerifierLength = 43;
	private const int MaxVerifierLength = 128;
	private const int DefaultVerifierLength = 64;

	/// <summary>
	/// Generates a cryptographically random code verifier
	/// </summary>
	public static string GenerateCodeVerifier(int length = DefaultVerifierLength)
	{
		if (length < MinVerifierLength || length > MaxVerifierLength)
		{
			throw new ArgumentOutOfRangeException(nameof(length),
				$"Length must be between {MinVerifierLength} and {MaxVerifierLength}");
		}

		var bytes = RandomNumberGenerator.GetBytes(length);
		return Base64UrlEncode(bytes);
	}

	/// <summary>
	/// Generates a code challenge from the code verifier using SHA256
	/// </summary>
	public static string GenerateCodeChallenge(string codeVerifier)
	{
		ArgumentNullException.ThrowIfNull(codeVerifier);

		using var sha256 = SHA256.Create();
		var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
		return Base64UrlEncode(challengeBytes);
	}

	/// <summary>
	/// Base64url encoding without padding
	/// </summary>
	private static string Base64UrlEncode(byte[] input)
	{
		return Convert.ToBase64String(input)
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
	}
}