using MediatR;

namespace ADHDChecklist.API.Features.Categories.UpdateCategory
{
    public record UpdateCategoryCommand(
        Guid CategoryId,
        string Name,
        string ColorHex,
        string? Icon,
        Guid UserId
    ) : IRequest<CategoryResponse?>;
}
