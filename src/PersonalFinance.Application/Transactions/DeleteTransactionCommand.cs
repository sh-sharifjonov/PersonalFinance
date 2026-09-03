using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common.Exceptions;
using PersonalFinance.Application.Common.Interfaces;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Transactions;

public record DeleteTransactionCommand(Guid Id) : IRequest;

public class DeleteTransactionCommandValidator : AbstractValidator<DeleteTransactionCommand>
{
    public DeleteTransactionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteTransactionCommandHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == _currentUser.UserId, cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException(nameof(Transaction), request.Id);
        }

        transaction.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
