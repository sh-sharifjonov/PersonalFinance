using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace PersonalFinance.Client.Auth;

public record LoginResult(bool Success, string? Error);

public class AuthService
{
    private const string AccessTokenKey = "auth:accessToken";

    private readonly HttpClient _http;
    private readonly IJSRuntime _jsRuntime;

    public AuthService(IHttpClientFactory httpClientFactory, IJSRuntime jsRuntime)
    {
        // Uses the unauthenticated "Api" client: login has no token yet, and routing it
        // through the authorized client would create a circular dependency (that client's
        // handler needs this service to look up the token).
        _http = httpClientFactory.CreateClient("Api");
        _jsRuntime = jsRuntime;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });

        if (!response.IsSuccessStatusCode)
        {
            return new LoginResult(false, $"Login failed ({(int)response.StatusCode}).");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (result is null)
        {
            return new LoginResult(false, "Unexpected response from server.");
        }

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, result.AccessToken);
        return new LoginResult(true, null);
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
    }

    private record AuthResponse(Guid UserId, string Email, string DisplayName, string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
}
