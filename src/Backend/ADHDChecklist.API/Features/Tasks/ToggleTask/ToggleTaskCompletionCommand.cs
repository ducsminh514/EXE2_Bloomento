using MediatR;

namespace ADHDChecklist.API.Features.Tasks.ToggleTask
{
    public record ToggleTaskCompletionCommand(Guid TaskId, Guid UserId) : IRequest<bool>;

}
