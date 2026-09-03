using System.Net.Http.Json;
using PersonalFinance.Client.Data;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Client.Sync;

public class SyncService
{
    private readonly HttpClient _http;
    private readonly IndexedDbService _db;
    private readonly OutboxService _outbox;
    private readonly SyncStateService _state;

    public SyncService(IHttpClientFactory httpClientFactory, IndexedDbService db, OutboxService outbox, SyncStateService state)
    {
        _http = httpClientFactory.CreateClient("ApiAuthorized");
        _db = db;
        _outbox = outbox;
        _state = state;
    }

    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await SyncLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            await PushAsync(cancellationToken);
            await PullAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // The Api being unreachable (offline, not yet started, CORS) is an expected,
            // recurring condition here, not a fatal error — log and retry on the next trigger.
            Console.Error.WriteLine($"Sync failed: {ex.Message}");
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task PushAsync(CancellationToken cancellationToken)
    {
        var entries = await _outbox.GetAllAsync();
        if (entries.Count == 0)
        {
            return;
        }

        var accounts = new List<Account>();
        var categories = new List<Category>();
        var transactions = new List<Transaction>();

        foreach (var entry in entries)
        {
            switch (entry.EntityStore)
            {
                case "accounts":
                    var account = await _db.GetAsync<Account>("accounts", entry.EntityId.ToString());
                    if (account is not null) accounts.Add(account);
                    break;
                case "categories":
                    var category = await _db.GetAsync<Category>("categories", entry.EntityId.ToString());
                    if (category is not null) categories.Add(category);
                    break;
                case "transactions":
                    var transaction = await _db.GetAsync<Transaction>("transactions", entry.EntityId.ToString());
                    if (transaction is not null) transactions.Add(transaction);
                    break;
            }
        }

        var payload = new
        {
            accounts = accounts.Select(ToAccountSyncDto),
            categories = categories.Select(ToCategorySyncDto),
            transactions = transactions.Select(ToTransactionSyncDto)
        };

        var response = await _http.PostAsJsonAsync("api/sync/push", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        foreach (var entry in entries)
        {
            await _outbox.RemoveAsync(entry.Id);
        }
    }

    private async Task PullAsync(CancellationToken cancellationToken)
    {
        var since = await _state.GetLastPulledAtAsync();
        var url = since is null
            ? "api/sync/pull"
            : $"api/sync/pull?since={Uri.EscapeDataString(since.Value.ToString("O"))}";

        var result = await _http.GetFromJsonAsync<PullResponse>(url, cancellationToken);
        if (result is null)
        {
            return;
        }

        foreach (var dto in result.Accounts)
        {
            await ApplyIfNewerAsync("accounts", dto.Id, dto.UpdatedAt, () => FromAccountSyncDto(dto));
        }

        foreach (var dto in result.Categories)
        {
            await ApplyIfNewerAsync("categories", dto.Id, dto.UpdatedAt, () => FromCategorySyncDto(dto));
        }

        foreach (var dto in result.Transactions)
        {
            await ApplyIfNewerAsync("transactions", dto.Id, dto.UpdatedAt, () => FromTransactionSyncDto(dto));
        }

        await _state.SetLastPulledAtAsync(result.ServerTime);
    }

    private async Task ApplyIfNewerAsync<T>(string storeName, Guid id, DateTime incomingUpdatedAt, Func<T> materialize)
        where T : Domain.Common.BaseEntity
    {
        var local = await _db.GetAsync<T>(storeName, id.ToString());
        if (local is not null && local.UpdatedAt > incomingUpdatedAt)
        {
            return;
        }

        await _db.PutAsync(storeName, materialize());
    }

    private static AccountSyncDto ToAccountSyncDto(Account a) =>
        new(a.Id, a.Name, a.Currency, a.InitialBalance, a.IsDeleted, a.CreatedAt, a.UpdatedAt);

    private static CategorySyncDto ToCategorySyncDto(Category c) =>
        new(c.Id, c.Name, c.Type, c.Icon, c.Color, c.IsDeleted, c.CreatedAt, c.UpdatedAt);

    private static TransactionSyncDto ToTransactionSyncDto(Transaction t) =>
        new(t.Id, t.AccountId, t.CategoryId, t.Type, t.Amount, t.Date, t.Note, t.IsDeleted, t.CreatedAt, t.UpdatedAt);

    private static Account FromAccountSyncDto(AccountSyncDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Currency = dto.Currency,
        InitialBalance = dto.InitialBalance,
        IsDeleted = dto.IsDeleted,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };

    private static Category FromCategorySyncDto(CategorySyncDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Type = dto.Type,
        Icon = dto.Icon,
        Color = dto.Color,
        IsDeleted = dto.IsDeleted,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };

    private static Transaction FromTransactionSyncDto(TransactionSyncDto dto) => new()
    {
        Id = dto.Id,
        AccountId = dto.AccountId,
        CategoryId = dto.CategoryId,
        Type = dto.Type,
        Amount = dto.Amount,
        Date = dto.Date,
        Note = dto.Note,
        IsDeleted = dto.IsDeleted,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };

    private record PullResponse(
        List<AccountSyncDto> Accounts,
        List<CategorySyncDto> Categories,
        List<TransactionSyncDto> Transactions,
        DateTime ServerTime);
}
