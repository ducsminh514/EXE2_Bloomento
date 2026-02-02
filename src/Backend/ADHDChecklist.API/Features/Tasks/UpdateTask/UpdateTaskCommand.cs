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
      bool IsCompleted,
      string? DopamineType,
      Guid UserId,
      Guid? AssignedUserId = null,
      bool IsShared = false
  ) : IRequest<TaskResponse?>;
}
