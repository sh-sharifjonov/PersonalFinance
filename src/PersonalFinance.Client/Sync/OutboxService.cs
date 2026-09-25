using PersonalFinance.Client.Data;

namespace PersonalFinance.Client.Sync;

public record OutboxEntry(string Id, string EntityStore, Guid EntityId);

public class OutboxService
{
    private const string StoreName = "outbox";

    private readonly IndexedDbService _db;

    public OutboxService(IndexedDbService db)
    {
        _db = db;
    }

    /// <summary>Raised after a local change is queued, so SyncService can push it soon.</summary>
    public event Action? Enqueued;

    public async Task EnqueueAsync(string entityStore, Guid entityId)
    {
        await _db.PutAsync(StoreName, new OutboxEntry($"{entityStore}:{entityId}", entityStore, entityId));
        Enqueued?.Invoke();
    }

    public Task<List<OutboxEntry>> GetAllAsync() => _db.GetAllAsync<OutboxEntry>(StoreName);

    public Task RemoveAsync(string id) => _db.DeleteAsync(StoreName, id);
}
