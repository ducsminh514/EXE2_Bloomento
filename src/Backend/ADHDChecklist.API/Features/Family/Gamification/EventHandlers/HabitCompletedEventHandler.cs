using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Shared.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Family.Gamification.EventHandlers;

public class HabitCompletedEventHandler : INotificationHandler<HabitCompletedEvent>
{
    private readonly AppDbContext _context;
    private readonly ILogger<HabitCompletedEventHandler> _logger;

    public HabitCompletedEventHandler(AppDbContext context, ILogger<HabitCompletedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task Handle(HabitCompletedEvent notification, CancellationToken cancellationToken)
    {
        var familyId = notification.FamilyId;

        if (!familyId.HasValue)
        {
            familyId = await _context.FamilyMembers
                .Where(fm => fm.UserId == notification.UserId)
                .Select(fm => (Guid?)fm.FamilyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!familyId.HasValue) return;

        var pointHistory = new FamilyPointHistory
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId.Value,
            UserId = notification.UserId,
            Amount = notification.Points,
            Source = "Habit",
            ReferenceId = notification.HabitId,
            CreatedAt = DateTime.UtcNow
        };

        _context.FamilyPointHistory.Add(pointHistory);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} earned {Points} points for habit {HabitId}", 
            notification.UserId, notification.Points, notification.HabitId);
    }
}
