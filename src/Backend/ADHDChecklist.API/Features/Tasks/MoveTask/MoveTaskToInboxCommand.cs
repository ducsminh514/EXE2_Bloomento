using MediatR;

namespace ADHDChecklist.API.Features.Tasks.MoveTask
{
    public record MoveTaskToInboxCommand(
      Guid TaskId,
      Guid UserId
  ) : IRequest<bool>;
}
