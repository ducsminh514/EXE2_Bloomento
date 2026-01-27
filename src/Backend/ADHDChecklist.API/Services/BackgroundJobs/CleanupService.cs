using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Services.BackgroundJobs;

public class CleanupService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(IServiceProvider serviceProvider, ILogger<CleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DeleteOldFreeTierTasks()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        // Find users who are in Free Tier
        // Note: In a real large-scale app, we would batch this or use a stored procedure.
        // For MVP, EF Core query is acceptable.
        
        var oldTasks = await context.Tasks
            .Include(t => t.User)
            .Where(t => t.User.SubscriptionTier == SubscriptionTier.Free 
                     && t.CreatedAt < cutoffDate)
            .ToListAsync();

        if (oldTasks.Any())
        {
            _logger.LogInformation("Found {Count} old tasks to delete for Free Tier users.", oldTasks.Count);
            
            context.Tasks.RemoveRange(oldTasks);
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully deleted {Count} old tasks.", oldTasks.Count);
        }
    }
}
