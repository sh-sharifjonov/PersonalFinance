using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Transactions;

public record TransactionDto(
    Guid Id,
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    DateOnly Date,
    string? Note,
    DateTime UpdatedAt);
