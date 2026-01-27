using ADHDChecklist.API.Data;
using ADHDChecklist.API.Services;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Services.BackgroundJobs;

public class ReminderJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderJob> _logger;

    public ReminderJob(IServiceProvider serviceProvider, ILogger<ReminderJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task CheckAndSendReminders()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // Window: checks tasks starting in next 15-30 minutes
        // This is a simple logic. In production, we'd need to track if reminder was already sent.
        // For MVP, since job runs every 15 mins, this window is roughly correct.
        var targetTimeStart = now.AddMinutes(15);
        var targetTimeEnd = now.AddMinutes(30);

        var tasksDue = await context.Tasks
            .Include(t => t.User)
            .Where(t => t.ScheduledDate == today 
                     && t.TimeBlockStart.HasValue
                     && t.TimeBlockStart.Value >= targetTimeStart 
                     && t.TimeBlockStart.Value <= targetTimeEnd
                     && !t.IsCompleted)
            .ToListAsync();

        if (tasksDue.Any())
        {
            _logger.LogInformation("Found {Count} tasks due for reminder.", tasksDue.Count);

            foreach (var task in tasksDue)
            {
                try 
                {
                    await emailService.SendEmailAsync(
                        task.User.Email, 
                        $"Nhắc nhở: {task.Title}", 
                        $"<p>Chào {task.User.FullName},</p><p>Công việc <strong>{task.Title}</strong> sắp bắt đầu lúc {task.TimeBlockStart}.</p><p>Hãy chuẩn bị nhé!</p>");
                        
                    _logger.LogInformation("Sent reminder for Task {TaskId} to {Email}", task.Id, task.User.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder for Task {TaskId}", task.Id);
                }
            }
        }
    }
}
