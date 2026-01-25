using MediatR;

namespace ADHDChecklist.API.Features.Categories.DeleteCategory
{
    public record DeleteCategoryCommand(Guid CategoryId, Guid UserId) : IRequest<bool>;

}
