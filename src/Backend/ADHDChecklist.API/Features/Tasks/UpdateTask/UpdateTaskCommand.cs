using ADHDChecklist.API.Shared.DTOs;
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.UpdateTask
{
    public record UpdateTaskCommand(
      Guid TaskId,
      string Title,
      string? Description,
      Guid? CategoryId,
      DateOnly ScheduledDate,
      TimeOnly? TimeBlockStart,
      TimeOnly? TimeBlockEnd,
      int? Duration,
      int Priority,
      Guid UserId
  ) : IRequest<TaskResponse?>;
}
