using MediatR;

namespace ADHDChecklist.API.Shared.Events;

public record TaskAssignedEvent(
    Guid TaskId,
    string TaskTitle,
    Guid AssignedUserId,
    Guid AssignedByUserId,
    string AssignedByUserName
) : INotification;
