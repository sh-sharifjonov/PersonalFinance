using System.Globalization;
using Microsoft.JSInterop;

namespace PersonalFinance.Client.Sync;

public class SyncStateService
{
    private const string LastPulledAtKey = "sync:lastPulledAt";

    private readonly IJSRuntime _jsRuntime;

    public SyncStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<DateTime?> GetLastPulledAtAsync()
    {
        var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", LastPulledAtKey);
        return value is null ? null : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public ValueTask SetLastPulledAtAsync(DateTime value) =>
        _jsRuntime.InvokeVoidAsync("localStorage.setItem", LastPulledAtKey, value.ToString("O", CultureInfo.InvariantCulture));
}
