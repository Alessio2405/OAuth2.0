using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2._0.Models
{
	/// <summary>
	/// Result of an OAuth 2.0 operation
	/// </summary>
	public class OAuth2Result
	{
		/// <summary>
		/// Indicates whether the operation was successful
		/// </summary>
		public bool Success { get; init; }

		/// <summary>
		/// The token response if successful
		/// </summary>
		public TokenResponse? Token { get; init; }

		/// <summary>
		/// Error code if the operation failed
		/// </summary>
		public string? Error { get; init; }

		/// <summary>
		/// Human-readable error description
		/// </summary>
		public string? ErrorDescription { get; init; }

		/// <summary>
		/// Creates a successful result
		/// </summary>
		public static OAuth2Result CreateSuccess(TokenResponse token)
		{
			ArgumentNullException.ThrowIfNull(token);
			return new OAuth2Result
			{
				Success = true,
				Token = token
			};
		}

		/// <summary>
		/// Creates an error result
		/// </summary>
		public static OAuth2Result CreateError(string error, string? description = null)
		{
			return new OAuth2Result
			{
				Success = false,
				Error = error,
				ErrorDescription = description
			};
		}
	}
}
