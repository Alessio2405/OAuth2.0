# OAuth2.0

A clean, generic OAuth 2.0 client library for .NET 8 with embedded browser support using WebView2.

## Features

- ✅ **Generic OAuth 2.0 Implementation** - Works with any OAuth 2.0 provider
- ✅ **Embedded Browser** - Built-in WebView2 browser for seamless authentication
- ✅ **PKCE Support** - Enhanced security with Proof Key for Code Exchange
- ✅ **Token Management** - Automatic token refresh and expiration handling
- ✅ **Thread-Safe** - Proper async/await patterns throughout
- ✅ **Extensible** - Support for custom parameters and endpoints

## Installation

### Build from Source

```bash
git clone https://github.com/Alessio2405/OAuth2.0.git
cd OAuth2.0
dotnet build
```

## Quick Start

```csharp
using OAuth2._0;
using OAuth2._0.Models;

// Configure OAuth2 client
var config = new OAuth2Config
{
    ClientId = "your-client-id",
    ClientSecret = "your-client-secret", // Optional for public clients
    AuthorizationUrl = "https://provider.com/oauth/authorize",
    TokenUrl = "https://provider.com/oauth/token",
    RedirectUri = "http://localhost:8080/callback",
    Scopes = "read write",
    UsePKCE = true // Recommended for security
};

// Create client and authenticate
using var client = new OAuth2Client(config);
var result = await client.AuthenticateAsync();

if (result.Success)
{
    Console.WriteLine($"Access Token: {result.Token.AccessToken}");
    
    // Use token for API calls
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", result.Token.AccessToken);
}
```

## Provider Examples

### GitHub OAuth

```csharp
var config = new OAuth2Config
{
    ClientId = "your-github-client-id",
    ClientSecret = "your-github-client-secret",
    AuthorizationUrl = "https://github.com/login/oauth/authorize",
    TokenUrl = "https://github.com/login/oauth/access_token",
    Scopes = "user repo",
    UsePKCE = false // GitHub doesn't support PKCE
};

using var client = new OAuth2Client(config);
var result = await client.AuthenticateAsync();
```

### Google OAuth

```csharp
var config = new OAuth2Config
{
    ClientId = "your-google-client-id",
    ClientSecret = "your-google-client-secret",
    AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth",
    TokenUrl = "https://oauth2.googleapis.com/token",
    RevocationUrl = "https://oauth2.googleapis.com/revoke",
    Scopes = "openid profile email",
    UsePKCE = true
};
```

### Microsoft Identity Platform

```csharp
var config = new OAuth2Config
{
    ClientId = "your-app-id",
    AuthorizationUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
    TokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
    Scopes = "user.read",
    UsePKCE = true,
    AdditionalAuthParams = new Dictionary<string, string>
    {
        ["prompt"] = "select_account"
    }
};
```

### Discord OAuth

```csharp
var config = new OAuth2Config
{
    ClientId = "your-discord-app-id",
    ClientSecret = "your-discord-app-secret",
    AuthorizationUrl = "https://discord.com/api/oauth2/authorize",
    TokenUrl = "https://discord.com/api/oauth2/token",
    Scopes = "identify email guilds",
    UsePKCE = false
};
```

### Spotify OAuth

```csharp
var config = new OAuth2Config
{
    ClientId = "your-spotify-client-id",
    ClientSecret = "your-spotify-client-secret",
    AuthorizationUrl = "https://accounts.spotify.com/authorize",
    TokenUrl = "https://accounts.spotify.com/api/token",
    Scopes = "user-read-private user-read-email",
    UsePKCE = true
};
```

## Advanced Usage

### Token Refresh

```csharp
if (!client.HasValidToken && client.CurrentToken?.RefreshToken != null)
{
    var refreshResult = await client.RefreshTokenAsync();
    if (refreshResult.Success)
    {
        Console.WriteLine("Token refreshed successfully");
    }
}
```

### Token Revocation

```csharp
await client.RevokeTokenAsync();
```

### Custom Parameters

```csharp
var config = new OAuth2Config
{
    ClientId = "your-client-id",
    AuthorizationUrl = "https://provider.com/oauth/authorize",
    TokenUrl = "https://provider.com/oauth/token",
    AdditionalAuthParams = new Dictionary<string, string>
    {
        ["custom_param"] = "value",
        ["prompt"] = "consent"
    },
    AdditionalTokenParams = new Dictionary<string, string>
    {
        ["custom_token_param"] = "value"
    }
};
```

## API Reference

### OAuth2Config Properties

- `ClientId` (required) - Your application's client ID
- `ClientSecret` - Client secret (optional for public clients)
- `AuthorizationUrl` (required) - Provider's authorization endpoint
- `TokenUrl` (required) - Provider's token endpoint
- `RedirectUri` - Callback URL (default: `http://localhost:8080/callback`)
- `Scopes` - Space-separated list of scopes
- `UsePKCE` - Enable PKCE for enhanced security (default: `true`)
- `RevocationUrl` - Token revocation endpoint (optional)
- `AdditionalAuthParams` - Custom authorization parameters
- `AdditionalTokenParams` - Custom token request parameters
- `TimeoutSeconds` - Request timeout (default: 30)

### OAuth2Client Methods

- `AuthenticateAsync()` - Starts the OAuth flow with embedded browser
- `RefreshTokenAsync()` - Refreshes the access token
- `RevokeTokenAsync()` - Revokes the current token
- `HasValidToken` - Checks if there's a valid unexpired token
- `AccessToken` - Gets the current access token
- `CurrentToken` - Gets the full token response

## Requirements

- .NET 8.0 or later
- Windows 7+ with WebView2 Runtime (pre-installed on Windows 10/11)
- WPF support for embedded browser

## License

MIT License - See [LICENSE](LICENSE) file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues and questions, please use the GitHub issues page.
