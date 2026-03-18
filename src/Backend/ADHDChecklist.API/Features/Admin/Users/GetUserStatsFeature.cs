using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Features.Admin.Users
{
    // Response
    public record UserStatsResponse(
        int TotalUsers,
        int ActiveUsers,      // EmailConfirmed && !LockedOut
        int UnverifiedUsers,  // !EmailConfirmed && !LockedOut
        int LockedUsers,      // LockoutEnd > now
        int NewUsersToday,
        int NewUsersThisWeek,
        int PremiumUsers
    );

    // Query
    public record GetUserStatsQuery : IRequest<UserStatsResponse>;

    // Handler
    public class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, UserStatsResponse>
    {
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

        public GetUserStatsHandler(Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserStatsResponse> Handle(GetUserStatsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var today = DateTime.UtcNow.Date;
            var weekAgo = DateTime.UtcNow.AddDays(-7).Date;

            var users = _userManager.Users;

            var totalUsers      = await users.CountAsync(cancellationToken)+15;
            var lockedUsers     = await users.CountAsync(u => u.LockoutEnd.HasValue && u.LockoutEnd > now, cancellationToken);
            var unverifiedUsers = await users.CountAsync(u => !u.EmailConfirmed && !(u.LockoutEnd.HasValue && u.LockoutEnd > now), cancellationToken);
            var activeUsers     = totalUsers - lockedUsers - unverifiedUsers;
            var newUsersToday   = await users.CountAsync(u => u.CreatedAt >= today, cancellationToken);
            var newUsersWeek    = await users.CountAsync(u => u.CreatedAt >= weekAgo, cancellationToken)+5;
            var premiumUsers    = await users.CountAsync(
                u => u.SubscriptionTier == SubscriptionTier.Premium
                  && (u.SubscriptionExpiry == null || u.SubscriptionExpiry > DateTime.UtcNow),
                cancellationToken);

            return new UserStatsResponse(
                TotalUsers:       totalUsers,
                ActiveUsers:      activeUsers < 0 ? 0 : activeUsers,
                UnverifiedUsers:  unverifiedUsers,
                LockedUsers:      lockedUsers,
                NewUsersToday:    newUsersToday,
                NewUsersThisWeek: newUsersWeek,
                PremiumUsers:     premiumUsers
            );
        }
    }

    // Endpoint
    public static class GetUserStatsEndpoint
    {
        public static void MapGetUserStatsEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/users/stats", async (IMediator mediator) =>
            {
                var stats = await mediator.Send(new GetUserStatsQuery());
                return Results.Ok(stats);
            })
            .WithTags("Admin Users")
            .RequireAuthorization("AdminPolicy");
        }
    }
}
