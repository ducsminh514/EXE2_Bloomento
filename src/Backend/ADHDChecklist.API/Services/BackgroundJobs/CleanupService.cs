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
        // POLICY CHANGE: Free Tier tasks are no longer deleted. They are hidden/archived instead.
        // This job is kept but disabled for deletion logic, potentially for future true archiving.
        _logger.LogInformation("CleanupService: Skipping execution. Hard delete policy disabled.");
        await Task.CompletedTask;
        
        /* 
        Legacy Deletion Logic (Disabled):
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);

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
        */
    }
}
