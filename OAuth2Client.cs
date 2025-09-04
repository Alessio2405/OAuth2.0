using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using OAuth2._0.Browser;
using OAuth2._0.Models;
using OAuth2._0.Utils;

namespace OAuth2._0;

/// <summary>
/// OAuth 2.0 client implementation with embedded browser support
/// </summary>
public class OAuth2Client : IDisposable
{
	private readonly HttpClient _httpClient;
	private readonly OAuth2Config _config;
	private readonly JsonSerializerOptions _jsonOptions;
	private TokenResponse? _currentToken;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the OAuth2Client
	/// </summary>
	public OAuth2Client(OAuth2Config config, HttpClient? httpClient = null)
	{
		_config = config ?? throw new ArgumentNullException(nameof(config));
		_httpClient = httpClient ?? new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
		};

		_jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};
	}

	/// <summary>
	/// Gets the current access token if valid
	/// </summary>
	public string? AccessToken =>
		_currentToken != null && !_currentToken.IsExpired
			? _currentToken.AccessToken
			: null;

	/// <summary>
	/// Gets the current token response
	/// </summary>
	public TokenResponse? CurrentToken => _currentToken;

	/// <summary>
	/// Checks if there's a valid access token
	/// </summary>
	public bool HasValidToken =>
		_currentToken != null && !_currentToken.IsExpired;

	/// <summary>
	/// Authenticates using the embedded browser (Authorization Code flow)
	/// </summary>
	public async Task<OAuth2Result> AuthenticateAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			// Generate state for CSRF protection
			var state = GenerateState();

			// Setup PKCE if enabled
			string? codeVerifier = null;
			string? codeChallenge = null;

			if (_config.UsePKCE)
			{
				codeVerifier = PKCEHelper.GenerateCodeVerifier();
				codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);
			}

			// Build authorization URL
			var authUrl = BuildAuthorizationUrl(state, codeChallenge);

			// Show browser window (must be on UI thread)
			var browserResult = await ShowBrowserAsync(authUrl);

			if (!browserResult.Success)
			{
				return OAuth2Result.CreateError(
					browserResult.Error ?? "user_cancelled",
					browserResult.ErrorDescription ?? "Authentication was cancelled");
			}

			// Validate state to prevent CSRF attacks
			if (browserResult.State != state)
			{
				return OAuth2Result.CreateError(
					"invalid_state",
					"State mismatch - possible CSRF attack");
			}

			// Exchange authorization code for tokens
			return await ExchangeCodeForTokenAsync(
				browserResult.AuthorizationCode!,
				codeVerifier,
				cancellationToken);
		}
		catch (Exception ex)
		{
			return OAuth2Result.CreateError("authentication_error", ex.Message);
		}
	}

	/// <summary>
	/// Refreshes the access token using the refresh token
	/// </summary>
	public async Task<OAuth2Result> RefreshTokenAsync(CancellationToken cancellationToken = default)
	{
		if (_currentToken?.RefreshToken == null)
		{
			return OAuth2Result.CreateError(
				"no_refresh_token",
				"No refresh token available");
		}

		try
		{
			var formData = new Dictionary<string, string>
			{
				["grant_type"] = "refresh_token",
				["refresh_token"] = _currentToken.RefreshToken,
				["client_id"] = _config.ClientId
			};

			if (!string.IsNullOrEmpty(_config.ClientSecret))
			{
				formData["client_secret"] = _config.ClientSecret;
			}

			if (!string.IsNullOrEmpty(_config.Scopes))
			{
				formData["scope"] = _config.Scopes;
			}

			var response = await SendTokenRequestAsync(formData, cancellationToken);

			if (response != null)
			{
				// Preserve refresh token if not included in response
				if (string.IsNullOrEmpty(response.RefreshToken) && !string.IsNullOrEmpty(_currentToken.RefreshToken))
				{
					response.RefreshToken = _currentToken.RefreshToken;
				}

				_currentToken = response;
				return OAuth2Result.CreateSuccess(response);
			}

			return OAuth2Result.CreateError("refresh_failed", "Failed to refresh token");
		}
		catch (Exception ex)
		{
			return OAuth2Result.CreateError("refresh_error", ex.Message);
		}
	}

	/// <summary>
	/// Revokes the current token
	/// </summary>
	public async Task<bool> RevokeTokenAsync(string? token = null, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(_config.RevocationUrl))
		{
			_currentToken = null;
			return true; // No revocation endpoint, just clear local token
		}

		token ??= _currentToken?.AccessToken;
		if (string.IsNullOrEmpty(token))
		{
			return true; // No token to revoke
		}

		try
		{
			var formData = new Dictionary<string, string>
			{
				["token"] = token,
				["token_type_hint"] = "access_token",
				["client_id"] = _config.ClientId
			};

			if (!string.IsNullOrEmpty(_config.ClientSecret))
			{
				formData["client_secret"] = _config.ClientSecret;
			}

			using var content = new FormUrlEncodedContent(formData);
			using var response = await _httpClient.PostAsync(_config.RevocationUrl, content, cancellationToken);

			_currentToken = null;
			return response.IsSuccessStatusCode;
		}
		catch
		{
			_currentToken = null;
			return false;
		}
	}

	private string GenerateState()
	{
		return Guid.NewGuid().ToString("N");
	}

	private string BuildAuthorizationUrl(string state, string? codeChallenge)
	{
		var queryParams = HttpUtility.ParseQueryString(string.Empty);
		queryParams["client_id"] = _config.ClientId;
		queryParams["redirect_uri"] = _config.RedirectUri;
		queryParams["response_type"] = "code";
		queryParams["state"] = state;

		if (!string.IsNullOrEmpty(_config.Scopes))
		{
			queryParams["scope"] = _config.Scopes;
		}

		if (_config.UsePKCE && !string.IsNullOrEmpty(codeChallenge))
		{
			queryParams["code_challenge"] = codeChallenge;
			queryParams["code_challenge_method"] = "S256";
		}

		// Add additional authorization parameters if provided
		if (_config.AdditionalAuthParams != null)
		{
			foreach (var param in _config.AdditionalAuthParams)
			{
				queryParams[param.Key] = param.Value;
			}
		}

		return $"{_config.AuthorizationUrl}?{queryParams}";
	}

	private async Task<BrowserResult> ShowBrowserAsync(string authUrl)
	{
		return await Task.Run(() =>
		{
			var browser = new OAuth2Browser(authUrl, _config.RedirectUri);
			var dialogResult = browser.ShowDialog();

			return new BrowserResult
			{
				Success = dialogResult == true,
				AuthorizationCode = browser.AuthorizationCode,
				State = browser.State,
				Error = browser.Error,
				ErrorDescription = browser.ErrorDescription
			};
		});
	}

	private async Task<OAuth2Result> ExchangeCodeForTokenAsync(
		string code,
		string? codeVerifier,
		CancellationToken cancellationToken)
	{
		try
		{
			var formData = new Dictionary<string, string>
			{
				["grant_type"] = "authorization_code",
				["code"] = code,
				["redirect_uri"] = _config.RedirectUri,
				["client_id"] = _config.ClientId
			};

			if (!string.IsNullOrEmpty(_config.ClientSecret))
			{
				formData["client_secret"] = _config.ClientSecret;
			}

			if (_config.UsePKCE && !string.IsNullOrEmpty(codeVerifier))
			{
				formData["code_verifier"] = codeVerifier;
			}

			// Add additional token parameters if provided
			if (_config.AdditionalTokenParams != null)
			{
				foreach (var param in _config.AdditionalTokenParams)
				{
					formData[param.Key] = param.Value;
				}
			}

			var response = await SendTokenRequestAsync(formData, cancellationToken);

			if (response != null)
			{
				_currentToken = response;
				return OAuth2Result.CreateSuccess(response);
			}

			return OAuth2Result.CreateError(
				"token_exchange_failed",
				"Failed to exchange code for token");
		}
		catch (Exception ex)
		{
			return OAuth2Result.CreateError("exchange_error", ex.Message);
		}
	}

	private async Task<TokenResponse?> SendTokenRequestAsync(
		Dictionary<string, string> formData,
		CancellationToken cancellationToken)
	{
		using var content = new FormUrlEncodedContent(formData);
		using var request = new HttpRequestMessage(HttpMethod.Post, _config.TokenUrl)
		{
			Content = content
		};

		// Set Accept header for JSON response
		request.Headers.Accept.Clear();
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		using var response = await _httpClient.SendAsync(request, cancellationToken);
		var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			return null;
		}

		var token = JsonSerializer.Deserialize<TokenResponse>(responseContent, _jsonOptions);

		if (token != null)
		{
			// Set expiration time
			if (token.ExpiresIn > 0)
			{
				token.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
			}
			else
			{
				// Default to 1 hour if no expiration provided
				token.ExpiresAtUtc = DateTime.UtcNow.AddHours(1);
			}
		}

		return token;
	}

	private record BrowserResult
	{
		public bool Success { get; init; }
		public string? AuthorizationCode { get; init; }
		public string? State { get; init; }
		public string? Error { get; init; }
		public string? ErrorDescription { get; init; }
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				_httpClient?.Dispose();
			}
			_disposed = true;
		}
	}
}
