using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Categories;

public record CategoryDto(Guid Id, string Name, CategoryType Type, string? Icon, string? Color, DateTime UpdatedAt);
