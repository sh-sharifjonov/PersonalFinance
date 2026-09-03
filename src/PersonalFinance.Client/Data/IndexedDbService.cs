using Microsoft.JSInterop;

namespace PersonalFinance.Client.Data;

public class IndexedDbService : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public IndexedDbService(IJSRuntime jsRuntime)
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() => jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./js/indexedDb.js")
            .AsTask());
    }

    public async Task<List<T>> GetAllAsync<T>(string storeName)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<List<T>>("getAll", storeName);
    }

    public async Task<T?> GetAsync<T>(string storeName, string id)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<T?>("get", storeName, id);
    }

    public async Task PutAsync<T>(string storeName, T item)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("put", storeName, item);
    }

    public async Task DeleteAsync(string storeName, string id)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("remove", storeName, id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
