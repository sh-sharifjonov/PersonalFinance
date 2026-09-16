using Microsoft.JSInterop;

namespace PersonalFinance.Client.Services;

/// <summary>Small per-device UI preferences (screen 1k) — not synced, stored in localStorage.</summary>
public class PreferencesService
{
    private const string DefaultAccountKey = "prefs:defaultAccountId";
    private const string FirstDayOfWeekKey = "prefs:firstDayOfWeek";
    private const string DashboardLayoutKey = "prefs:dashboardLayout";

    private readonly IJSRuntime _jsRuntime;

    public PreferencesService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<Guid?> GetDefaultAccountIdAsync()
    {
        var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", DefaultAccountKey);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public ValueTask SetDefaultAccountIdAsync(Guid id) =>
        _jsRuntime.InvokeVoidAsync("localStorage.setItem", DefaultAccountKey, id.ToString());

    /// <summary>"monday" or "sunday".</summary>
    public async Task<string> GetFirstDayOfWeekAsync() =>
        await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", FirstDayOfWeekKey) ?? "monday";

    public ValueTask SetFirstDayOfWeekAsync(string value) =>
        _jsRuntime.InvokeVoidAsync("localStorage.setItem", FirstDayOfWeekKey, value);

    /// <summary>Which dashboard layout variant (1a/1b/1c) to show by default.</summary>
    public async Task<string> GetDashboardLayoutAsync() =>
        await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", DashboardLayoutKey) ?? "cards";

    public ValueTask SetDashboardLayoutAsync(string value) =>
        _jsRuntime.InvokeVoidAsync("localStorage.setItem", DashboardLayoutKey, value);
}
