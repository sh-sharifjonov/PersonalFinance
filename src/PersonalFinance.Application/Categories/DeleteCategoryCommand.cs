using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common.Exceptions;
using PersonalFinance.Application.Common.Interfaces;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Categories;

public record DeleteCategoryCommand(Guid Id) : IRequest;

public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteCategoryCommandHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == _currentUser.UserId, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        category.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
