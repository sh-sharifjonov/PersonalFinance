using Microsoft.JSInterop;

namespace PersonalFinance.Client.Services;

// Per-device UI preferences. Kept in localStorage, not synced: they describe this
// device's display, not the user's financial data.
public class PreferencesService
{
    private const string ThemeKey = "pref:theme";
    private const string CurrencyKey = "pref:currency";
    private const string DefaultAccountKey = "pref:defaultAccount";

    private readonly IJSRuntime _jsRuntime;

    public PreferencesService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> GetDarkThemeAsync() =>
        await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThemeKey) == "dark";

    public async Task SetDarkThemeAsync(bool dark)
    {
        var theme = dark ? "dark" : "light";
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeKey, theme);
        await _jsRuntime.InvokeVoidAsync("pfApp.setTheme", theme);
    }

    public Task<string?> GetCurrencyAsync() =>
        _jsRuntime.InvokeAsync<string?>("localStorage.getItem", CurrencyKey).AsTask();

    public ValueTask SetCurrencyAsync(string currency) =>
        _jsRuntime.InvokeVoidAsync("localStorage.setItem", CurrencyKey, currency);

    public async Task<Guid?> GetDefaultAccountIdAsync()
    {
        var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", DefaultAccountKey);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public ValueTask SetDefaultAccountIdAsync(Guid? id) => id is null
        ? _jsRuntime.InvokeVoidAsync("localStorage.removeItem", DefaultAccountKey)
        : _jsRuntime.InvokeVoidAsync("localStorage.setItem", DefaultAccountKey, id.Value.ToString());
}
