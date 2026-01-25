using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Categories.GetCategories
{
    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryResponse>>
    {
        private readonly AppDbContext _context;

        public GetCategoriesQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Categories
                .Where(c => c.UserId == request.UserId)
                .OrderBy(c => c.OrderIndex)
                .Select(c => new CategoryResponse(
                    c.Id,
                    c.Name,
                    c.ColorHex,
                    c.Icon,
                    c.OrderIndex,
                    c.Tasks.Count(t => t.DeletedAt == null)
                ))
                .ToListAsync(cancellationToken);
        }
    }

}
