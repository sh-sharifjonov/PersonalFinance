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

    public Task EnqueueAsync(string entityStore, Guid entityId) =>
        _db.PutAsync(StoreName, new OutboxEntry($"{entityStore}:{entityId}", entityStore, entityId));

    public Task<List<OutboxEntry>> GetAllAsync() => _db.GetAllAsync<OutboxEntry>(StoreName);

    public Task RemoveAsync(string id) => _db.DeleteAsync(StoreName, id);
}
