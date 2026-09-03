using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Client.Sync;

// Mirrors PersonalFinance.Application.Sync's wire contract. Duplicated rather than
// shared via a project reference so the client doesn't pull in the server's
// MediatR/FluentValidation/EF Core-facing Application layer.
public record AccountSyncDto(
    Guid Id,
    string Name,
    string Currency,
    decimal InitialBalance,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CategorySyncDto(
    Guid Id,
    string Name,
    CategoryType Type,
    string? Icon,
    string? Color,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TransactionSyncDto(
    Guid Id,
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    DateOnly Date,
    string? Note,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt);
