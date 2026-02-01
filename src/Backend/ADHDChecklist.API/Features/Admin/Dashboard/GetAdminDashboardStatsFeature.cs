using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Features.Admin.Dashboard
{
    // Response
    public record AdminDashboardStatsResponse(
        int TotalUsers,
        int NewUsersToday,
        int TotalArticles,
        int TotalComments,
        int HiddenComments,
        int TotalCategories
    );

    // Query
    public record GetAdminDashboardStatsQuery : IRequest<AdminDashboardStatsResponse>;

    // Handler
    public class GetAdminDashboardStatsHandler : IRequestHandler<GetAdminDashboardStatsQuery, AdminDashboardStatsResponse>
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

        public GetAdminDashboardStatsHandler(AppDbContext context, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<AdminDashboardStatsResponse> Handle(GetAdminDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;

            // User Stats
            // Note: _userManager.Users is IQueryable
            var totalUsers = await _userManager.Users.CountAsync(cancellationToken);
            var newUsers = await _userManager.Users.CountAsync(u => u.CreatedAt >= today, cancellationToken);

            // Knowledge Stats
            var totalArticles = await _context.Articles.CountAsync(cancellationToken);
            var totalCategories = await _context.KnowledgeCategories.CountAsync(cancellationToken);
            var totalComments = await _context.ArticleComments.CountAsync(cancellationToken);
            var hiddenComments = await _context.ArticleComments.CountAsync(c => c.IsHidden, cancellationToken);

            return new AdminDashboardStatsResponse(
                TotalUsers: totalUsers,
                NewUsersToday: newUsers,
                TotalArticles: totalArticles,
                TotalComments: totalComments,
                HiddenComments: hiddenComments, // Usually moderators want to see what is hidden or reported.
                TotalCategories: totalCategories
            );
        }
    }

    // Endpoint
    public static class GetAdminDashboardStatsEndpoint
    {
        public static void MapGetAdminDashboardStatsEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/dashboard/stats", async (IMediator mediator) =>
            {
                var stats = await mediator.Send(new GetAdminDashboardStatsQuery());
                return Results.Ok(stats);
            })
            .WithTags("Admin Dashboard")
            .RequireAuthorization("AdminPolicy");
        }
    }
}
