using PersonalFinance.Client.Data;
using PersonalFinance.Client.Sync;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Client.Services;

public class LocalAccountService
{
    private const string StoreName = "accounts";

    private readonly IndexedDbService _db;
    private readonly OutboxService _outbox;

    public LocalAccountService(IndexedDbService db, OutboxService outbox)
    {
        _db = db;
        _outbox = outbox;
    }

    public async Task<List<Account>> GetAllAsync()
    {
        var accounts = await _db.GetAllAsync<Account>(StoreName);
        return accounts.Where(a => !a.IsDeleted).OrderBy(a => a.Name).ToList();
    }

    public async Task<Account> CreateAsync(string name, string currency, decimal initialBalance)
    {
        var account = new Account
        {
            Name = name,
            Currency = currency,
            InitialBalance = initialBalance
        };

        await _db.PutAsync(StoreName, account);
        await _outbox.EnqueueAsync(StoreName, account.Id);
        return account;
    }

    public async Task UpdateAsync(Guid id, string name, string currency, decimal initialBalance)
    {
        var account = await _db.GetAsync<Account>(StoreName, id.ToString());
        if (account is null)
        {
            return;
        }

        account.Name = name;
        account.Currency = currency;
        account.InitialBalance = initialBalance;
        account.UpdatedAt = DateTime.UtcNow;
        await _db.PutAsync(StoreName, account);
        await _outbox.EnqueueAsync(StoreName, account.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var account = await _db.GetAsync<Account>(StoreName, id.ToString());
        if (account is null)
        {
            return;
        }

        account.IsDeleted = true;
        account.UpdatedAt = DateTime.UtcNow;
        await _db.PutAsync(StoreName, account);
        await _outbox.EnqueueAsync(StoreName, account.Id);
    }
}
