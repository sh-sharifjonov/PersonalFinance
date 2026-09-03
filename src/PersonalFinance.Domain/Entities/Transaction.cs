using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid AccountId { get; set; }

    public Guid CategoryId { get; set; }

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }

    public string? Note { get; set; }

    public User? User { get; set; }

    public Account? Account { get; set; }

    public Category? Category { get; set; }
}
