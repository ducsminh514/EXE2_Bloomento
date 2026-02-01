using MediatR;
using Microsoft.AspNetCore.Identity;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Features.Admin.Users
{
    // Command
    public record ToggleUserLockCommand(Guid UserId) : IRequest<bool>;

    // Handler
    public class ToggleUserLockHandler : IRequestHandler<ToggleUserLockCommand, bool>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ToggleUserLockHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> Handle(ToggleUserLockCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null) return false;

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null); // Unlock
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100)); // Lock forever
            }

            return true;
        }
    }

    // Endpoint
    public static class ToggleUserLockEndpoint
    {
        public static void MapToggleUserLockEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/admin/users/{id:guid}/lock", async (IMediator mediator, Guid id) =>
            {
                var success = await mediator.Send(new ToggleUserLockCommand(id));
                return success ? Results.Ok() : Results.NotFound();
            })
            .WithTags("Admin Users")
            .RequireAuthorization("AdminPolicy");
        }
    }
}
