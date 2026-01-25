using MediatR;

namespace ADHDChecklist.API.Features.Categories.CreateCategory
{
    public record CreateCategoryCommand(
        string Name,
        string ColorHex,
        string? Icon,
        Guid UserId
    ) : IRequest<CategoryResponse>;
}
