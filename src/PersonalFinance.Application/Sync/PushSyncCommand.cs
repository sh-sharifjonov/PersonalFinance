using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common.Interfaces;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Sync;

public record PushSyncCommand(
    List<AccountSyncDto> Accounts,
    List<CategorySyncDto> Categories,
    List<TransactionSyncDto> Transactions) : IRequest;

public class PushSyncCommandHandler : IRequestHandler<PushSyncCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PushSyncCommandHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(PushSyncCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        // Accounts and categories are applied (and saved) before transactions so that
        // transactions referencing brand-new accounts/categories from the same batch
        // can be validated against already-persisted rows.
        await ApplyAccountsAsync(request.Accounts, userId, cancellationToken);
        await _context.SaveChangesForSyncAsync(cancellationToken);

        await ApplyCategoriesAsync(request.Categories, userId, cancellationToken);
        await _context.SaveChangesForSyncAsync(cancellationToken);

        await ApplyTransactionsAsync(request.Transactions, userId, cancellationToken);
        await _context.SaveChangesForSyncAsync(cancellationToken);
    }

    private async Task ApplyAccountsAsync(List<AccountSyncDto> items, Guid userId, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var ids = items.Select(i => i.Id).ToList();
        var existing = await _context.Accounts.IgnoreQueryFilters()
            .Where(a => ids.Contains(a.Id) && a.UserId == userId)
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        foreach (var dto in items)
        {
            if (existing.TryGetValue(dto.Id, out var account))
            {
                if (dto.UpdatedAt < account.UpdatedAt)
                {
                    continue;
                }

                account.Name = dto.Name;
                account.Currency = dto.Currency;
                account.InitialBalance = dto.InitialBalance;
                account.IsDeleted = dto.IsDeleted;
                account.UpdatedAt = dto.UpdatedAt;
            }
            else
            {
                _context.Accounts.Add(new Account
                {
                    Id = dto.Id,
                    UserId = userId,
                    Name = dto.Name,
                    Currency = dto.Currency,
                    InitialBalance = dto.InitialBalance,
                    IsDeleted = dto.IsDeleted,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
                });
            }
        }
    }

    private async Task ApplyCategoriesAsync(List<CategorySyncDto> items, Guid userId, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var ids = items.Select(i => i.Id).ToList();
        var existing = await _context.Categories.IgnoreQueryFilters()
            .Where(c => ids.Contains(c.Id) && c.UserId == userId)
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        foreach (var dto in items)
        {
            if (existing.TryGetValue(dto.Id, out var category))
            {
                if (dto.UpdatedAt < category.UpdatedAt)
                {
                    continue;
                }

                category.Name = dto.Name;
                category.Type = dto.Type;
                category.Icon = dto.Icon;
                category.Color = dto.Color;
                category.IsDeleted = dto.IsDeleted;
                category.UpdatedAt = dto.UpdatedAt;
            }
            else
            {
                _context.Categories.Add(new Category
                {
                    Id = dto.Id,
                    UserId = userId,
                    Name = dto.Name,
                    Type = dto.Type,
                    Icon = dto.Icon,
                    Color = dto.Color,
                    IsDeleted = dto.IsDeleted,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
                });
            }
        }
    }

    private async Task ApplyTransactionsAsync(List<TransactionSyncDto> items, Guid userId, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var ids = items.Select(i => i.Id).ToList();
        var existing = await _context.Transactions.IgnoreQueryFilters()
            .Where(t => ids.Contains(t.Id) && t.UserId == userId)
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var accountIds = await _context.Accounts.IgnoreQueryFilters()
            .Where(a => a.UserId == userId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
        var categoryIds = await _context.Categories.IgnoreQueryFilters()
            .Where(c => c.UserId == userId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var accountIdSet = accountIds.ToHashSet();
        var categoryIdSet = categoryIds.ToHashSet();

        foreach (var dto in items)
        {
            // Skip transactions whose account/category isn't (yet) known for this user
            // rather than failing the whole batch — the client will retry once the
            // missing parent has synced.
            if (!accountIdSet.Contains(dto.AccountId) || !categoryIdSet.Contains(dto.CategoryId))
            {
                continue;
            }

            if (existing.TryGetValue(dto.Id, out var transaction))
            {
                if (dto.UpdatedAt < transaction.UpdatedAt)
                {
                    continue;
                }

                transaction.AccountId = dto.AccountId;
                transaction.CategoryId = dto.CategoryId;
                transaction.Type = dto.Type;
                transaction.Amount = dto.Amount;
                transaction.Date = dto.Date;
                transaction.Note = dto.Note;
                transaction.IsDeleted = dto.IsDeleted;
                transaction.UpdatedAt = dto.UpdatedAt;
            }
            else
            {
                _context.Transactions.Add(new Transaction
                {
                    Id = dto.Id,
                    UserId = userId,
                    AccountId = dto.AccountId,
                    CategoryId = dto.CategoryId,
                    Type = dto.Type,
                    Amount = dto.Amount,
                    Date = dto.Date,
                    Note = dto.Note,
                    IsDeleted = dto.IsDeleted,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
                });
            }
        }
    }
}
