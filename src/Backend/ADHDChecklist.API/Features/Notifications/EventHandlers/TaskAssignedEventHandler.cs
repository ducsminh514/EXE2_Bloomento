using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Shared.Events;
using MediatR;

namespace ADHDChecklist.API.Features.Notifications.EventHandlers;

public class TaskAssignedEventHandler : INotificationHandler<TaskAssignedEvent>
{
    private readonly AppDbContext _context;
    private readonly ILogger<TaskAssignedEventHandler> _logger;

    public TaskAssignedEventHandler(AppDbContext context, ILogger<TaskAssignedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task Handle(TaskAssignedEvent notification, CancellationToken cancellationToken)
    {
        // Don't notify if assigned to self
        if (notification.AssignedUserId == notification.AssignedByUserId)
        {
            return;
        }

        var notif = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = notification.AssignedUserId,
            Title = "Công việc mới 📝",
            Message = $"{notification.AssignedByUserName} đã giao cho bạn việc: {notification.TaskTitle}",
            Type = "TaskAssignment",
            ReferenceId = notification.TaskId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification created for user {UserId} about task {TaskId}", notification.AssignedUserId, notification.TaskId);
    }
}
