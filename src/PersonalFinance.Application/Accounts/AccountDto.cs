namespace PersonalFinance.Application.Accounts;

public record AccountDto(Guid Id, string Name, string Currency, decimal InitialBalance, DateTime UpdatedAt);
