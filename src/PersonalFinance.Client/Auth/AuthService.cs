using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace PersonalFinance.Client.Auth;

public record LoginResult(bool Success, string? Error);

public class AuthService
{
    private const string AccessTokenKey = "auth:accessToken";
    private const string RefreshTokenKey = "auth:refreshToken";

    private readonly HttpClient _http;
    private readonly IJSRuntime _jsRuntime;
    private readonly JwtAuthenticationStateProvider _authStateProvider;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthService(IHttpClientFactory httpClientFactory, IJSRuntime jsRuntime, JwtAuthenticationStateProvider authStateProvider)
    {
        // Uses the unauthenticated "Api" client: login/refresh have no valid access token
        // yet, and routing them through the authorized client would create a circular
        // dependency (that client's handler needs this service to look up the token).
        _http = httpClientFactory.CreateClient("Api");
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
    }

    public Task<LoginResult> LoginAsync(string email, string password) =>
        AuthenticateAsync("api/auth/login", new { email, password });

    public Task<LoginResult> RegisterAsync(string email, string password, string displayName) =>
        AuthenticateAsync("api/auth/register", new { email, password, displayName });

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        _authStateProvider.NotifyAuthenticationChanged();
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
    }

    // Called by AuthorizationMessageHandler when a request comes back 401. Rotates the
    // refresh token server-side (see RefreshTokenCommand), so only one caller should be
    // mid-refresh at a time — concurrent 401s would otherwise each burn a rotation.
    public async Task<bool> RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            var response = await _http.PostAsJsonAsync("api/auth/refresh", new { refreshToken });
            if (!response.IsSuccessStatusCode)
            {
                await LogoutAsync();
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result is null)
            {
                await LogoutAsync();
                return false;
            }

            await StoreTokensAsync(result);
            return true;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<LoginResult> AuthenticateAsync(string requestUri, object payload)
    {
        var response = await _http.PostAsJsonAsync(requestUri, payload);

        if (!response.IsSuccessStatusCode)
        {
            return new LoginResult(false, await ExtractErrorMessageAsync(response));
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (result is null)
        {
            return new LoginResult(false, "Unexpected response from server.");
        }

        await StoreTokensAsync(result);
        return new LoginResult(true, null);
    }

    private async Task StoreTokensAsync(AuthResponse response)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, response.AccessToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, response.RefreshToken);
        _authStateProvider.NotifyAuthenticationChanged();
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            if (problem?.Errors is { Count: > 0 })
            {
                return string.Join(" ", problem.Errors.SelectMany(kv => kv.Value));
            }

            if (!string.IsNullOrWhiteSpace(problem?.Title))
            {
                return problem.Title;
            }
        }
        catch
        {
            // Response body wasn't the JSON shape we expected — fall back below.
        }

        return $"Request failed ({(int)response.StatusCode}).";
    }

    private record AuthResponse(Guid UserId, string Email, string DisplayName, string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private record ErrorResponse(string? Title, Dictionary<string, string[]>? Errors);
}
