using Microsoft.JSInterop;

namespace PersonalFinance.Client.Services;

/// <summary>Saves generated text (CSV/JSON exports) to the user's device as a file download.</summary>
public class FileDownloadService : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public FileDownloadService(IJSRuntime jsRuntime)
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() => jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./js/fileDownload.js")
            .AsTask());
    }

    public async Task DownloadTextAsync(string fileName, string content, string mimeType)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("downloadText", fileName, content, mimeType);
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
