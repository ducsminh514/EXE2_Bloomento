using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Categories.UpdateCategory
{
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryResponse?>
    {
        private readonly AppDbContext _context;

        public UpdateCategoryCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CategoryResponse?> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == request.UserId, cancellationToken);

            if (category == null) return null;

            category.Name = request.Name;
            category.ColorHex = request.ColorHex;
            category.Icon = request.Icon;

            await _context.SaveChangesAsync(cancellationToken);

            var taskCount = await _context.Tasks
                .CountAsync(t => t.CategoryId == category.Id && t.DeletedAt == null, cancellationToken);

            return new CategoryResponse(
                category.Id,
                category.Name,
                category.ColorHex,
                category.Icon,
                category.OrderIndex,
                taskCount
            );
        }
    }
}
