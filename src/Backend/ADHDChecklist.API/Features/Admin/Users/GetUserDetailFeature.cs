using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Features.Admin.Users
{
    // Response Model
    public record UserDetailResponse(
        string Id,
        string Email,
        string FullName,
        string SubscriptionTier,
        bool IsPremium,
        bool IsEmailVerified,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        bool IsLockedOut,
        DateTimeOffset? LockoutEnd,
        int TaskCount,
        int CompletedTaskCount,
        int ArticleReadCount, // Placeholder for now or actual if tracking exists
        string CurrentStreak // Formatted string e.g. "5 days"
    );

    // Query
    public record GetUserDetailQuery(Guid UserId) : IRequest<UserDetailResponse?>;

    // Handler
    public class GetUserDetailHandler : IRequestHandler<GetUserDetailQuery, UserDetailResponse?>
    {
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public GetUserDetailHandler(Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<UserDetailResponse?> Handle(GetUserDetailQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.Users
                .Include(u => u.Tasks)
                .Include(u => u.Habits)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null) return null;

            var taskCount = user.Tasks.Count;
            var completedTaskCount = user.Tasks.Count(t => t.IsCompleted);
            
            // Calculate Streak (Simple logic: Check habits or login streak if available. Using Habits longest streak for now)
            var maxStreak = user.Habits.Any() ? user.Habits.Max(h => h.LongestStreak) : 0;

            return new UserDetailResponse(
                user.Id.ToString(),
                user.Email ?? "",
                user.FullName ?? "",
                user.SubscriptionTier.ToString(),
                user.SubscriptionTier == SubscriptionTier.Premium && (user.SubscriptionExpiry == null || user.SubscriptionExpiry > DateTime.UtcNow),
                user.EmailConfirmed,
                user.CreatedAt,
                user.LastLoginAt,
                user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                user.LockoutEnd,
                taskCount,
                completedTaskCount,
                0, // Article read count not tracked yet
                $"{maxStreak} days"
            );
        }
    }

    // Endpoint
    public static class GetUserDetailEndpoint
    {
        public static void MapGetUserDetailEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/users/{id:guid}", async (IMediator mediator, Guid id) =>
            {
                var result = await mediator.Send(new GetUserDetailQuery(id));
                return result != null ? Results.Ok(result) : Results.NotFound();
            })
            .WithTags("Admin Users")
            .RequireAuthorization("AdminPolicy");
        }
    }
}
