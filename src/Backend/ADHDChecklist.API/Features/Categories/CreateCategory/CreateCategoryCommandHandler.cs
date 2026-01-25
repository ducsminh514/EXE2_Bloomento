using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Categories.CreateCategory
{
    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryResponse>
    {
        private readonly AppDbContext _context;

        public CreateCategoryCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var maxOrder = await _context.Categories
                .Where(c => c.UserId == request.UserId)
                .MaxAsync(c => (int?)c.OrderIndex, cancellationToken) ?? 0;

            var category = new Category
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Name = request.Name,
                ColorHex = request.ColorHex,
                Icon = request.Icon,
                OrderIndex = maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return new CategoryResponse(
                category.Id,
                category.Name,
                category.ColorHex,
                category.Icon,
                category.OrderIndex,
                0
            );
        }
    }

}
