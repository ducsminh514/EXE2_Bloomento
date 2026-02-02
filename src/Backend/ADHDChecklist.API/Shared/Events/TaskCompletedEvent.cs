using MediatR;

namespace ADHDChecklist.API.Shared.Events;

public record TaskCompletedEvent(
    Guid TaskId,
    Guid UserId,
    Guid? FamilyId,
    int Points
) : INotification;
