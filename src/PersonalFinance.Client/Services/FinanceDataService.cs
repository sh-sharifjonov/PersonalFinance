using PersonalFinance.Client.Sync;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Client.Services;

// Read-side helper for pages: one IndexedDB read of everything, plus the aggregates the
// dashboards need. Data sets are per-user and small, so aggregating in memory is fine.
public class FinanceDataService
{
    private readonly LocalAccountService _accounts;
    private readonly LocalCategoryService _categories;
    private readonly LocalTransactionService _transactions;
    private readonly OutboxService _outbox;
    private readonly PreferencesService _preferences;

    public FinanceDataService(
        LocalAccountService accounts,
        LocalCategoryService categories,
        LocalTransactionService transactions,
        OutboxService outbox,
        PreferencesService preferences)
    {
        _accounts = accounts;
        _categories = categories;
        _transactions = transactions;
        _outbox = outbox;
        _preferences = preferences;
    }

    public async Task<FinanceSnapshot> LoadAsync()
    {
        var accounts = await _accounts.GetAllAsync();
        var categories = await _categories.GetAllAsync();
        var transactions = await _transactions.GetAllAsync();
        var pending = await _outbox.GetAllAsync();
        var currency = await _preferences.GetCurrencyAsync();

        return new FinanceSnapshot(
            accounts,
            categories,
            transactions,
            pending.Where(e => e.EntityStore == "transactions").Select(e => e.EntityId).ToHashSet(),
            currency ?? accounts.FirstOrDefault()?.Currency ?? "USD");
    }
}

public sealed class FinanceSnapshot
{
    private readonly Dictionary<Guid, Account> _accountById;
    private readonly Dictionary<Guid, Category> _categoryById;
    private readonly HashSet<Guid> _pendingTransactionIds;

    public FinanceSnapshot(
        List<Account> accounts,
        List<Category> categories,
        List<Transaction> transactions,
        HashSet<Guid> pendingTransactionIds,
        string currency)
    {
        Accounts = accounts;
        Categories = categories;
        Transactions = transactions
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToList();
        Currency = currency;
        _pendingTransactionIds = pendingTransactionIds;
        _accountById = accounts.ToDictionary(a => a.Id);
        _categoryById = categories.ToDictionary(c => c.Id);
    }

    public List<Account> Accounts { get; }

    public List<Category> Categories { get; }

    /// <summary>Newest first.</summary>
    public List<Transaction> Transactions { get; }

    /// <summary>Display currency for cross-account totals (no FX conversion is done).</summary>
    public string Currency { get; }

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public Account? AccountOf(Transaction t) => _accountById.GetValueOrDefault(t.AccountId);

    public Category? CategoryOf(Transaction t) => _categoryById.GetValueOrDefault(t.CategoryId);

    public string CurrencyOf(Transaction t) => AccountOf(t)?.Currency ?? Currency;

    public bool IsPending(Transaction t) => _pendingTransactionIds.Contains(t.Id);

    /// <summary>Transactions whose account still exists (orphans of deleted accounts don't count toward totals).</summary>
    public IEnumerable<Transaction> Live => Transactions.Where(t => _accountById.ContainsKey(t.AccountId));

    public IEnumerable<Transaction> Between(DateOnly from, DateOnly to) =>
        Live.Where(t => t.Date >= from && t.Date <= to);

    public static decimal SignedAmount(Transaction t) =>
        t.Type == TransactionType.Income ? t.Amount : -t.Amount;

    public decimal Balance(Account account) =>
        account.InitialBalance + Live.Where(t => t.AccountId == account.Id).Sum(SignedAmount);

    public decimal TotalBalance => Accounts.Sum(Balance);

    public decimal BalanceAt(DateOnly date) =>
        Accounts.Sum(a => a.InitialBalance) + Live.Where(t => t.Date <= date).Sum(SignedAmount);

    public static decimal Income(IEnumerable<Transaction> source) =>
        source.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);

    public static decimal Expense(IEnumerable<Transaction> source) =>
        source.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

    public static (DateOnly From, DateOnly To) MonthRange(int year, int month)
    {
        var from = new DateOnly(year, month, 1);
        return (from, from.AddMonths(1).AddDays(-1));
    }
}
