using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common.Interfaces;

namespace PersonalFinance.Application.Categories;

public record GetCategoriesQuery : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCategoriesQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Where(c => c.UserId == _currentUser.UserId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.Icon, c.Color, c.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
