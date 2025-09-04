using System;
using System.Collections.Specialized;
using System.Web;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace OAuth2._0.Browser;

/// <summary>
/// OAuth 2.0 authentication browser window
/// </summary>
public partial class OAuth2Browser : Window
{
	private readonly string _authorizationUrl;
	private readonly string _redirectUri;

	public string? AuthorizationCode { get; private set; }
	public string? State { get; private set; }
	public string? Error { get; private set; }
	public string? ErrorDescription { get; private set; }

	public OAuth2Browser(string authorizationUrl, string redirectUri)
	{
		InitializeComponent();
		_authorizationUrl = authorizationUrl ?? throw new ArgumentNullException(nameof(authorizationUrl));
		_redirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			StatusText.Text = "Initializing secure browser...";

			// Initialize WebView2
			var env = await CoreWebView2Environment.CreateAsync();
			await WebView.EnsureCoreWebView2Async(env);

			// Configure WebView2 settings for security
			var settings = WebView.CoreWebView2.Settings;
			settings.AreDevToolsEnabled = false;
			settings.IsStatusBarEnabled = false;
			settings.IsPasswordAutosaveEnabled = false;
			settings.IsGeneralAutofillEnabled = false;
			settings.AreDefaultScriptDialogsEnabled = true;

			// Clear cookies and cache for fresh authentication
			WebView.CoreWebView2.CookieManager.DeleteAllCookies();

			StatusText.Text = "Navigating to authorization server...";

			// Navigate to authorization URL
			WebView.Source = new Uri(_authorizationUrl);
		}
		catch (Exception ex)
		{
			Error = "initialization_error";
			ErrorDescription = $"Failed to initialize browser: {ex.Message}";
			DialogResult = false;
			Close();
		}
	}

	private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
	{
		if (string.IsNullOrEmpty(e.Uri)) return;

		// Update status
		var uri = new Uri(e.Uri);
		StatusText.Text = $"Loading: {uri.Host}";

		// Check if this is our redirect URL
		if (!e.Uri.StartsWith(_redirectUri, StringComparison.OrdinalIgnoreCase))
			return;

		try
		{
			// Parse parameters from query and fragment
			var parameters = ParseAuthorizationResponse(uri);

			// Check for error
			if (parameters.ContainsKey("error"))
			{
				Error = parameters["error"];
				ErrorDescription = parameters.TryGetValue("error_description", out var desc) ? desc : null;
				DialogResult = false;
				Close();
				return;
			}

			// Get authorization code
			if (parameters.TryGetValue("code", out var code))
			{
				AuthorizationCode = code;
				State = parameters.TryGetValue("state", out var state) ? state : null;
				DialogResult = true;
				Close();
				return;
			}

			// No code or error found
			Error = "invalid_response";
			ErrorDescription = "No authorization code or error in response";
			DialogResult = false;
			Close();
		}
		catch (Exception ex)
		{
			Error = "parse_error";
			ErrorDescription = ex.Message;
			DialogResult = false;
			Close();
		}
	}

	private static Dictionary<string, string> ParseAuthorizationResponse(Uri uri)
	{
		var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		// Parse query string
		if (!string.IsNullOrEmpty(uri.Query))
		{
			var queryParams = HttpUtility.ParseQueryString(uri.Query);
			AddParameters(queryParams, parameters);
		}

		// Parse fragment (for implicit flow or hybrid flow)
		if (!string.IsNullOrEmpty(uri.Fragment))
		{
			var fragment = uri.Fragment.TrimStart('#');
			var fragmentParams = HttpUtility.ParseQueryString(fragment);
			AddParameters(fragmentParams, parameters);
		}

		return parameters;
	}

	private static void AddParameters(NameValueCollection source, Dictionary<string, string> target)
	{
		foreach (string? key in source.AllKeys)
		{
			if (key != null && !target.ContainsKey(key))
			{
				target[key] = source[key] ?? string.Empty;
			}
		}
	}
}
