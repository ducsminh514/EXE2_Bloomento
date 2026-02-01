using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Features.Admin.Dashboard
{
    // Response Models
    public record AdminDashboardChartsResponse(
        List<UserGrowthData> UserGrowth,
        List<CategoryDistributionData> CategoryDistribution
    );

    public record UserGrowthData(DateTime Date, int Count);
    public record CategoryDistributionData(string CategoryName, int ArticleCount);

    // Query
    public record GetAdminDashboardChartsQuery : IRequest<AdminDashboardChartsResponse>;

    // Handler
    public class GetAdminDashboardChartsHandler : IRequestHandler<GetAdminDashboardChartsQuery, AdminDashboardChartsResponse>
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

        public GetAdminDashboardChartsHandler(AppDbContext context, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<AdminDashboardChartsResponse> Handle(GetAdminDashboardChartsQuery request, CancellationToken cancellationToken)
        {
            // 1. User Growth (Last 7 Days)
            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.UtcNow.Date.AddDays(-i)).Reverse().ToList();
            var userGrowth = new List<UserGrowthData>();

            // Note: This is not optimal for large datasets (querying in loop or grouping in memory if provider doesn't support Date diff).
            // For now, simpler approach: Get all users created in last 7 days then group in memory.
            var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-7);
            var recentUsers = await _userManager.Users
                .Where(u => u.CreatedAt >= sevenDaysAgo)
                .Select(u => u.CreatedAt)
                .ToListAsync(cancellationToken);

            foreach (var date in last7Days)
            {
                var count = recentUsers.Count(u => u.Date == date);
                userGrowth.Add(new UserGrowthData(date, count));
            }

            // 2. Category Distribution (Articles per Category)
            var categoryStats = await _context.Articles
                .Include(a => a.Category)
                .GroupBy(a => a.Category.Name)
                .Select(g => new CategoryDistributionData(g.Key, g.Count()))
                .ToListAsync(cancellationToken);

            return new AdminDashboardChartsResponse(userGrowth, categoryStats);
        }
    }

    // Endpoint
    public static class GetAdminDashboardChartsEndpoint
    {
        public static void MapGetAdminDashboardChartsEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/dashboard/charts", async (IMediator mediator) =>
            {
                var charts = await mediator.Send(new GetAdminDashboardChartsQuery());
                return Results.Ok(charts);
            })
            .WithTags("Admin Dashboard")
            .RequireAuthorization("AdminPolicy");
        }
    }
}
