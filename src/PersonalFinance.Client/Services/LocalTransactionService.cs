using PersonalFinance.Client.Data;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Client.Services;

public class LocalTransactionService
{
    private const string StoreName = "transactions";

    private readonly IndexedDbService _db;

    public LocalTransactionService(IndexedDbService db)
    {
        _db = db;
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
        return transaction;
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
    }
}
