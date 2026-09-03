using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common.Interfaces;

namespace PersonalFinance.Application.Accounts;

public record GetAccountsQuery : IRequest<List<AccountDto>>;

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, List<AccountDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAccountsQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .Where(a => a.UserId == _currentUser.UserId)
            .OrderBy(a => a.Name)
            .Select(a => new AccountDto(a.Id, a.Name, a.Currency, a.InitialBalance, a.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
