using PersonalFinance.Domain.Common;

namespace PersonalFinance.Domain.Entities;

public class Account : BaseEntity
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Currency { get; set; } = "USD";

    public decimal InitialBalance { get; set; }

    public User? User { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
