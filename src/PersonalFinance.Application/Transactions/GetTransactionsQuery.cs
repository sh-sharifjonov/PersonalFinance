using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common.Interfaces;

namespace PersonalFinance.Application.Transactions;

public record GetTransactionsQuery(Guid? AccountId = null) : IRequest<List<TransactionDto>>;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, List<TransactionDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTransactionsQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Transactions.Where(t => t.UserId == _currentUser.UserId);

        if (request.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == request.AccountId.Value);
        }

        return await query
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionDto(t.Id, t.AccountId, t.CategoryId, t.Type, t.Amount, t.Date, t.Note, t.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
