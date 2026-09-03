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

    public Task<LoginResult> LoginAsync(string email, string password) =>
        AuthenticateAsync("api/auth/login", new { email, password });

    public Task<LoginResult> RegisterAsync(string email, string password, string displayName) =>
        AuthenticateAsync("api/auth/register", new { email, password, displayName });

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
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

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, result.AccessToken);
        return new LoginResult(true, null);
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
