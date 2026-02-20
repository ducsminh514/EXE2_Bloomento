using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Shared.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Family.Gamification.EventHandlers;

public class TaskCompletedEventHandler : INotificationHandler<TaskCompletedEvent>
{
    private readonly AppDbContext _context;
    private readonly ILogger<TaskCompletedEventHandler> _logger;

    public TaskCompletedEventHandler(AppDbContext context, ILogger<TaskCompletedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
    {
        var familyId = notification.FamilyId;

        // If task doesn't have familyId, try to find user's family
        if (!familyId.HasValue)
        {
            familyId = await _context.FamilyMembers
                .Where(fm => fm.UserId == notification.UserId)
                .Select(fm => (Guid?)fm.FamilyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // 1. Always Award Personal XP
        var user = await _context.Users.FindAsync(new object[] { notification.UserId }, cancellationToken);
        if (user != null)
        {
            user.TotalXp += notification.Points;
            // _context.Users.Update(user); // Entity tracked by default
        }

        // 2. Family Points Logic (Only if User is in Family)
        if (!familyId.HasValue)
        {
            familyId = await _context.FamilyMembers
                .Where(fm => fm.UserId == notification.UserId)
                .Select(fm => (Guid?)fm.FamilyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (familyId.HasValue)
        {
            var pointHistory = new FamilyPointHistory
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId.Value,
                UserId = notification.UserId,
                Amount = notification.Points,
                Source = "Task",
                ReferenceId = notification.TaskId,
                CreatedAt = DateTime.UtcNow
            };

            _context.FamilyPointHistory.Add(pointHistory);
            
             _logger.LogInformation("User {UserId} earned {Points} points in family {FamilyId}",
                notification.UserId, notification.Points, familyId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
