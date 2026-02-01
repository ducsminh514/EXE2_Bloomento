using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Features.Admin.Users
{
    // Response Models
    public record UserListResponse(
        List<UserDto> Users,
        int TotalCount,
        int CurrentPage,
        int PageSize
    );

    public record UserDto(
        string Id,
        string Email,
        string FullName,
        string SubscriptionTier,
        bool IsPremium,
        bool IsEmailVerified,
        DateTime CreatedAt,
        bool IsLockedOut
    );

    // Query
    public record GetUsersQuery(int Page = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<UserListResponse>;

    // Handler
    public class GetUsersHandler : IRequestHandler<GetUsersQuery, UserListResponse>
    {
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

        public GetUsersHandler(Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserListResponse> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(u => u.Email!.ToLower().Contains(term) || u.FullName.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UserDto(
                    u.Id.ToString(),
                    u.Email ?? "",
                    u.FullName ?? "",
                    u.SubscriptionTier.ToString(),
                    u.SubscriptionTier == SubscriptionTier.Premium && (u.SubscriptionExpiry == null || u.SubscriptionExpiry > DateTime.UtcNow),
                    u.EmailConfirmed, // Using Identity's EmailConfirmed as IsEmailVerified proxy
                    u.CreatedAt,
                    u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow
                ))
                .ToListAsync(cancellationToken);

            return new UserListResponse(users, totalCount, request.Page, request.PageSize);
        }
    }

    // Endpoint
    public static class GetUsersEndpoint
    {
        public static void MapGetUsersEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/users", async (IMediator mediator, int page = 1, int pageSize = 10, string? search = null) =>
            {
                var result = await mediator.Send(new GetUsersQuery(page, pageSize, search));
                return Results.Ok(result);
            })
            .WithTags("Admin Users")
            .RequireAuthorization("AdminPolicy");
        }
    }
}
