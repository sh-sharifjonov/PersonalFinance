using Microsoft.JSInterop;

namespace PersonalFinance.Client.Sync;

public class ConnectivityService : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private DotNetObjectReference<ConnectivityService>? _selfRef;

    public ConnectivityService(IJSRuntime jsRuntime)
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() => jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./js/connectivity.js")
            .AsTask());
    }

    public event Action<bool>? OnlineStatusChanged;

    public bool IsOnline { get; private set; } = true;

    public async Task InitializeAsync()
    {
        var module = await _moduleTask.Value;
        IsOnline = await module.InvokeAsync<bool>("isOnline");

        _selfRef = DotNetObjectReference.Create(this);
        await module.InvokeVoidAsync("registerConnectivityCallback", _selfRef);
    }

    [JSInvokable]
    public void OnOnline()
    {
        IsOnline = true;
        OnlineStatusChanged?.Invoke(true);
    }

    [JSInvokable]
    public void OnOffline()
    {
        IsOnline = false;
        OnlineStatusChanged?.Invoke(false);
    }

    public async ValueTask DisposeAsync()
    {
        _selfRef?.Dispose();

        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
