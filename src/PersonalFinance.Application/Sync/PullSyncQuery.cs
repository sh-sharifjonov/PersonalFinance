using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common.Interfaces;

namespace PersonalFinance.Application.Sync;

public record PullSyncQuery(DateTime? Since) : IRequest<SyncPullResultDto>;

public record SyncPullResultDto(
    List<AccountSyncDto> Accounts,
    List<CategorySyncDto> Categories,
    List<TransactionSyncDto> Transactions,
    DateTime ServerTime);

public class PullSyncQueryHandler : IRequestHandler<PullSyncQuery, SyncPullResultDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PullSyncQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<SyncPullResultDto> Handle(PullSyncQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var since = request.Since ?? DateTime.MinValue;

        // Captured before querying so a record written mid-pull is simply picked up
        // again on the next pull instead of being missed by a cursor from the future.
        var serverTime = DateTime.UtcNow;

        // IgnoreQueryFilters: soft-deleted rows must still be pulled so other devices
        // apply the deletion locally instead of keeping a stale copy forever.
        var accounts = await _context.Accounts.IgnoreQueryFilters()
            .Where(a => a.UserId == userId && a.UpdatedAt > since)
            .Select(a => new AccountSyncDto(a.Id, a.Name, a.Currency, a.InitialBalance, a.IsDeleted, a.CreatedAt, a.UpdatedAt))
            .ToListAsync(cancellationToken);

        var categories = await _context.Categories.IgnoreQueryFilters()
            .Where(c => c.UserId == userId && c.UpdatedAt > since)
            .Select(c => new CategorySyncDto(c.Id, c.Name, c.Type, c.Icon, c.Color, c.IsDeleted, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(cancellationToken);

        var transactions = await _context.Transactions.IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.UpdatedAt > since)
            .Select(t => new TransactionSyncDto(t.Id, t.AccountId, t.CategoryId, t.Type, t.Amount, t.Date, t.Note, t.IsDeleted, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new SyncPullResultDto(accounts, categories, transactions, serverTime);
    }
}
