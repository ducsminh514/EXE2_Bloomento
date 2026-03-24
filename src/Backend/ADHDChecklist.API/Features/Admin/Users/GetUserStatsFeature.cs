using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Data;

namespace ADHDChecklist.API.Features.Admin.Users
{
    // Response
    public record UserStatsResponse(
        int TotalUsers,
        int ActiveUsers,
        int UnverifiedUsers,
        int LockedUsers,
        int NewUsersToday,
        int NewUsersThisWeek,
        int PremiumUsers,
        // --- New: Growth & Revenue ---
        int FamilyAccounts,
        double ConversionRate,
        long TotalRevenuePaid,
        long RevenueThisMonth,
        int ActiveLast7Days,
        double AvgTasksPerUser,
        double MonthlyGrowthRate
    );

    // Query
    public record GetUserStatsQuery : IRequest<UserStatsResponse>;

    // Handler
    public class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, UserStatsResponse>
    {
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public GetUserStatsHandler(
            Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<UserStatsResponse> Handle(GetUserStatsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var today = DateTime.UtcNow.Date;
            var weekAgo = DateTime.UtcNow.AddDays(-7).Date;
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = monthStart.AddMonths(-1);

            var users = _userManager.Users;

            // --- Existing stats (giữ fake data) ---
            var totalUsers      = await users.CountAsync(cancellationToken)+15;
            var lockedUsers     = await users.CountAsync(u => u.LockoutEnd.HasValue && u.LockoutEnd > now, cancellationToken);
            var unverifiedUsers = await users.CountAsync(u => !u.EmailConfirmed && !(u.LockoutEnd.HasValue && u.LockoutEnd > now), cancellationToken);
            var activeUsers     = totalUsers - lockedUsers - unverifiedUsers;
            var newUsersToday   = await users.CountAsync(u => u.CreatedAt >= today, cancellationToken);
            var newUsersWeek    = await users.CountAsync(u => u.CreatedAt >= weekAgo, cancellationToken)-2;
            var premiumUsers    = await users.CountAsync(
                u => u.SubscriptionTier == SubscriptionTier.Premium
                  && (u.SubscriptionExpiry == null || u.SubscriptionExpiry > DateTime.UtcNow),
                cancellationToken);

            // --- New: Family Accounts ---
            var familyAccounts = await _context.Families.CountAsync(cancellationToken);

            // --- New: Conversion Rate (Free → Paid %) ---
            var familyUsers = await users.CountAsync(
                u => u.SubscriptionTier == SubscriptionTier.Family
                  && (u.SubscriptionExpiry == null || u.SubscriptionExpiry > DateTime.UtcNow),
                cancellationToken);
            var paidUsers = premiumUsers + familyUsers;
            var conversionRate = totalUsers > 0 ? Math.Round((double)paidUsers / totalUsers * 100, 1) : 0;

            // --- New: Revenue từ Transaction ---
            var totalRevenuePaid = await _context.Transactions
                .Where(t => t.Status == "PAID")
                .SumAsync(t => (long)t.Amount, cancellationToken);

            var revenueThisMonth = await _context.Transactions
                .Where(t => t.Status == "PAID" && t.CreatedAt >= monthStart)
                .SumAsync(t => (long)t.Amount, cancellationToken);

            // --- New: Active Last 7 Days (dựa vào LastLoginAt) ---
            var activeLast7Days = await users.CountAsync(
                u => u.LastLoginAt.HasValue && u.LastLoginAt >= weekAgo, cancellationToken);

            // --- New: Avg Tasks/User (tháng này) ---
            var tasksThisMonth = await _context.Tasks
                .CountAsync(t => t.CreatedAt >= monthStart, cancellationToken);
            var activeUsersCount = Math.Max(activeUsers, 1); // tránh chia 0
            var avgTasksPerUser = Math.Round((double)tasksThisMonth / activeUsersCount, 1);

            // --- New: Monthly Growth Rate ---
            var newUsersThisMonth = await users.CountAsync(u => u.CreatedAt >= monthStart, cancellationToken);
            var newUsersLastMonth = await users.CountAsync(
                u => u.CreatedAt >= lastMonthStart && u.CreatedAt < monthStart, cancellationToken);
            var monthlyGrowthRate = newUsersLastMonth > 0
                ? Math.Round(((double)newUsersThisMonth / newUsersLastMonth - 1) * 100, 1)
                : (newUsersThisMonth > 0 ? 100.0 : 0.0);

            return new UserStatsResponse(
                TotalUsers:        totalUsers,
                ActiveUsers:       activeUsers < 0 ? 0 : activeUsers,
                UnverifiedUsers:   unverifiedUsers,
                LockedUsers:       lockedUsers,
                NewUsersToday:     newUsersToday,
                NewUsersThisWeek:  newUsersWeek,
                PremiumUsers:      premiumUsers,
                FamilyAccounts:    familyAccounts,
                ConversionRate:    conversionRate,
                TotalRevenuePaid:  totalRevenuePaid,
                RevenueThisMonth:  revenueThisMonth,
                ActiveLast7Days:   activeLast7Days,
                AvgTasksPerUser:   avgTasksPerUser,
                MonthlyGrowthRate: monthlyGrowthRate
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
