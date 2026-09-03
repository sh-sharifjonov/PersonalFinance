using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common.Exceptions;
using PersonalFinance.Application.Common.Interfaces;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Transactions;

public record UpdateTransactionCommand(
    Guid Id,
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    DateOnly Date,
    string? Note) : IRequest;

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Date).NotEqual(default(DateOnly));
        RuleFor(x => x.Note).MaximumLength(512);
    }
}

public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateTransactionCommandHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException(nameof(Transaction), request.Id);
        }

        var accountExists = await _context.Accounts
            .AnyAsync(a => a.Id == request.AccountId && a.UserId == userId, cancellationToken);
        if (!accountExists)
        {
            throw new NotFoundException(nameof(Account), request.AccountId);
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException(nameof(Category), request.CategoryId);
        }

        transaction.AccountId = request.AccountId;
        transaction.CategoryId = request.CategoryId;
        transaction.Type = request.Type;
        transaction.Amount = request.Amount;
        transaction.Date = request.Date;
        transaction.Note = request.Note;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
