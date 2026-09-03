using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace PersonalFinance.Client.Auth;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string AccessTokenKey = "auth:accessToken";
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private readonly IJSRuntime _jsRuntime;

    // Reads the token directly from localStorage instead of depending on AuthService:
    // AuthService needs to call NotifyAuthenticationChanged() after login/logout, and
    // depending on it here would create a DI cycle.
    public JwtAuthenticationStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(Anonymous);
        }

        var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthenticationChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();

        foreach (var pair in keyValuePairs)
        {
            if (pair.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in pair.Value.EnumerateArray())
                {
                    yield return new Claim(pair.Key, item.ToString());
                }
            }
            else
            {
                yield return new Claim(pair.Key, pair.Value.ToString());
            }
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty
        };

        return Convert.FromBase64String(padded);
    }
}
