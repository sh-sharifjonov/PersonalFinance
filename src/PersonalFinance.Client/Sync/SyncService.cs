using System.Net.Http.Json;
using PersonalFinance.Client.Auth;
using PersonalFinance.Client.Data;
using PersonalFinance.Client.Services;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Client.Sync;

public class SyncService
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    // Short debounce so a burst of local edits goes out as one push batch.
    private static readonly TimeSpan PushDebounce = TimeSpan.FromSeconds(1.5);

    private readonly HttpClient _http;
    private readonly IndexedDbService _db;
    private readonly OutboxService _outbox;
    private readonly SyncStateService _state;
    private readonly ConnectivityService _connectivity;
    private readonly AuthService _auth;
    private readonly AppState _appState;

    private CancellationTokenSource? _scheduled;

    public SyncService(
        IHttpClientFactory httpClientFactory,
        IndexedDbService db,
        OutboxService outbox,
        SyncStateService state,
        ConnectivityService connectivity,
        AuthService auth,
        AppState appState)
    {
        _http = httpClientFactory.CreateClient("ApiAuthorized");
        _db = db;
        _outbox = outbox;
        _state = state;
        _connectivity = connectivity;
        _auth = auth;
        _appState = appState;
    }

    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    /// <summary>Raised whenever IsSyncing / HasError / PendingCount / IsOnline may have changed.</summary>
    public event Action? StateChanged;

    public bool IsSyncing { get; private set; }

    public bool HasError { get; private set; }

    public int PendingCount { get; private set; }

    public bool IsOnline => _connectivity.IsOnline;

    public async Task StartAsync()
    {
        await _connectivity.InitializeAsync();
        _connectivity.OnlineStatusChanged += isOnline =>
        {
            StateChanged?.Invoke();
            if (isOnline)
            {
                _ = SyncAsync();
            }
        };

        _outbox.Enqueued += OnLocalChangeQueued;

        await RefreshPendingCountAsync();

        if (_connectivity.IsOnline)
        {
            _ = SyncAsync();
        }
    }

    public Task<DateTime?> GetLastPulledAtAsync() => _state.GetLastPulledAtAsync();

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        // Nothing to talk to the server with yet — not an error, just wait for login.
        if (string.IsNullOrEmpty(await _auth.GetAccessTokenAsync()))
        {
            return;
        }

        if (!await SyncLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        CancelScheduled();
        IsSyncing = true;
        StateChanged?.Invoke();

        try
        {
            var pushed = await PushAsync(cancellationToken);
            await PullAsync(cancellationToken);
            HasError = !pushed;
        }
        catch (Exception ex)
        {
            // The Api being unreachable (offline, not yet started, CORS) is an expected,
            // recurring condition here, not a fatal error — log and retry later. Stdout, not
            // stderr: Blazor WASM pops its "unhandled error" bar on anything written to stderr.
            Console.WriteLine($"Sync failed: {ex.Message}");
            HasError = true;
        }
        finally
        {
            SyncLock.Release();
        }

        await RefreshPendingCountAsync();
        IsSyncing = false;
        StateChanged?.Invoke();

        if (HasError && _connectivity.IsOnline)
        {
            Schedule(RetryDelay);
        }
    }

    private async void OnLocalChangeQueued()
    {
        await RefreshPendingCountAsync();
        StateChanged?.Invoke();

        if (_connectivity.IsOnline && !IsSyncing)
        {
            Schedule(PushDebounce);
        }
    }

    private void Schedule(TimeSpan delay)
    {
        CancelScheduled();
        var cts = new CancellationTokenSource();
        _scheduled = cts;

        _ = Task.Delay(delay, cts.Token).ContinueWith(
            t =>
            {
                if (!t.IsCanceled && _connectivity.IsOnline)
                {
                    _ = SyncAsync();
                }
            },
            TaskScheduler.Default);
    }

    private void CancelScheduled()
    {
        _scheduled?.Cancel();
        _scheduled = null;
    }

    private async Task RefreshPendingCountAsync()
    {
        try
        {
            PendingCount = (await _outbox.GetAllAsync()).Count;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Reading outbox failed: {ex.Message}");
        }
    }

    /// <returns>false when the server rejected the batch (entries stay queued).</returns>
    private async Task<bool> PushAsync(CancellationToken cancellationToken)
    {
        var entries = await _outbox.GetAllAsync();
        if (entries.Count == 0)
        {
            return true;
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
            return false;
        }

        foreach (var entry in entries)
        {
            await _outbox.RemoveAsync(entry.Id);
        }

        return true;
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

        var changed = false;

        foreach (var dto in result.Accounts)
        {
            changed |= await ApplyIfNewerAsync("accounts", dto.Id, dto.UpdatedAt, () => FromAccountSyncDto(dto));
        }

        foreach (var dto in result.Categories)
        {
            changed |= await ApplyIfNewerAsync("categories", dto.Id, dto.UpdatedAt, () => FromCategorySyncDto(dto));
        }

        foreach (var dto in result.Transactions)
        {
            changed |= await ApplyIfNewerAsync("transactions", dto.Id, dto.UpdatedAt, () => FromTransactionSyncDto(dto));
        }

        await _state.SetLastPulledAtAsync(result.ServerTime);

        if (changed)
        {
            _appState.NotifyDataChanged();
        }
    }

    // Returns true only when the incoming row is strictly newer: our own just-pushed rows
    // come back in the pull with an equal UpdatedAt and shouldn't count as a remote change.
    private async Task<bool> ApplyIfNewerAsync<T>(string storeName, Guid id, DateTime incomingUpdatedAt, Func<T> materialize)
        where T : Domain.Common.BaseEntity
    {
        var local = await _db.GetAsync<T>(storeName, id.ToString());
        if (local is not null && local.UpdatedAt > incomingUpdatedAt)
        {
            return false;
        }

        await _db.PutAsync(storeName, materialize());
        return local is null || local.UpdatedAt < incomingUpdatedAt;
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
