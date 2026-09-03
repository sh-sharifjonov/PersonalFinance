using PersonalFinance.Client.Data;
using PersonalFinance.Client.Sync;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Client.Services;

public class LocalTransactionService
{
    private const string StoreName = "transactions";

    private readonly IndexedDbService _db;
    private readonly OutboxService _outbox;

    public LocalTransactionService(IndexedDbService db, OutboxService outbox)
    {
        _db = db;
        _outbox = outbox;
    }

    public async Task<List<Transaction>> GetAllAsync(Guid? accountId = null)
    {
        var transactions = await _db.GetAllAsync<Transaction>(StoreName);
        var query = transactions.Where(t => !t.IsDeleted);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        return query.OrderByDescending(t => t.Date).ToList();
    }

    public async Task<Transaction> CreateAsync(
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string? note)
    {
        var transaction = new Transaction
        {
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Date = date,
            Note = note
        };

        await _db.PutAsync(StoreName, transaction);
        await _outbox.EnqueueAsync(StoreName, transaction.Id);
        return transaction;
    }

    public async Task UpdateAsync(
        Guid id,
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string? note)
    {
        var transaction = await _db.GetAsync<Transaction>(StoreName, id.ToString());
        if (transaction is null)
        {
            return;
        }

        transaction.AccountId = accountId;
        transaction.CategoryId = categoryId;
        transaction.Type = type;
        transaction.Amount = amount;
        transaction.Date = date;
        transaction.Note = note;
        transaction.UpdatedAt = DateTime.UtcNow;
        await _db.PutAsync(StoreName, transaction);
        await _outbox.EnqueueAsync(StoreName, transaction.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var transaction = await _db.GetAsync<Transaction>(StoreName, id.ToString());
        if (transaction is null)
        {
            return;
        }

        transaction.IsDeleted = true;
        transaction.UpdatedAt = DateTime.UtcNow;
        await _db.PutAsync(StoreName, transaction);
        await _outbox.EnqueueAsync(StoreName, transaction.Id);
    }
}
