using FluentValidation;
using MediatR;
using PersonalFinance.Application.Common.Interfaces;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Categories;

public record CreateCategoryCommand(Guid Id, string Name, CategoryType Type, string? Icon, string? Color) : IRequest<Guid>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Icon).MaximumLength(64);
        RuleFor(x => x.Color).MaximumLength(16);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateCategoryCommandHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Id = request.Id,
            UserId = _currentUser.UserId,
            Name = request.Name,
            Type = request.Type,
            Icon = request.Icon,
            Color = request.Color
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
