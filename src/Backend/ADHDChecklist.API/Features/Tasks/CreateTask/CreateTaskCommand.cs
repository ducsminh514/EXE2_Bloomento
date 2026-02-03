using ADHDChecklist.API.Shared.DTOs;
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.CreateTask
{
    public record CreateTaskCommand(
        string Title,
        string? Description,
        Guid? CategoryId,
        DateOnly ScheduledDate,
        TimeOnly? TimeBlockStart,
        TimeOnly? TimeBlockEnd,
        int? Duration,
        int Priority,
        bool IsRecurring,
        string? RecurrencePattern,
        Guid UserId,
        string? DopamineType = "Low",
        Guid? FamilyId = null,
        Guid? AssignedUserId = null,
        bool IsShared = false,
        bool IsMandatory = false
    ) : IRequest<TaskResponse>;
}
