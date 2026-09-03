using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common.Exceptions;
using PersonalFinance.Application.Common.Interfaces;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Accounts;

public record DeleteAccountCommand(Guid Id) : IRequest;

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteAccountCommandHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == _currentUser.UserId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.Id);
        }

        account.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
