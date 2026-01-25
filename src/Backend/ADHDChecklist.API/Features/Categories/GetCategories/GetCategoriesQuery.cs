using MediatR;

namespace ADHDChecklist.API.Features.Categories.GetCategories
{
    public record GetCategoriesQuery(Guid UserId) : IRequest<List<CategoryResponse>>;

