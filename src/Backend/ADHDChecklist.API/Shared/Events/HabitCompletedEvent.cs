using MediatR;

namespace ADHDChecklist.API.Shared.Events;

public record HabitCompletedEvent(
    Guid HabitId,
    Guid UserId,
    Guid? FamilyId,
    int Points
) : INotification;
