using MediatR;

namespace ADHDChecklist.API.Features.Tasks.DeleteTask
{
    public record DeleteTaskCommand(Guid TaskId, Guid UserId) : IRequest<bool>;

}
