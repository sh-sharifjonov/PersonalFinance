using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<Account> Accounts { get; }

    DbSet<Category> Categories { get; }

    DbSet<Transaction> Transactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Skips the CreatedAt/UpdatedAt auto-stamping: sync push writes client-authoritative
    // timestamps that must survive as-is for Last-Write-Wins to compare correctly.
    Task<int> SaveChangesForSyncAsync(CancellationToken cancellationToken = default);
}
