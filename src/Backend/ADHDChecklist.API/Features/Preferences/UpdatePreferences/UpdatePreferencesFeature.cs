using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Preferences.UpdatePreferences;

// Command
public record UpdatePreferencesCommand(
    Guid UserId,
    string? ThemeColor,
    string? ThemeMode // "light" or "dark"
) : IRequest<bool>;

// Handler
public class UpdatePreferencesHandler : IRequestHandler<UpdatePreferencesCommand, bool>
{
    private readonly AppDbContext _context;

    public UpdatePreferencesHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdatePreferencesCommand request, CancellationToken ct)
    {
        // Find existing preference or create new
        var preference = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        if (preference == null)
        {
            preference = new UserPreference
            {
                UserId = request.UserId
                // CreatedAt not present in entity
            };
            _context.UserPreferences.Add(preference);
        }

        // Update fields if provided
        if (request.ThemeColor != null) preference.CustomPrimaryColor = request.ThemeColor;
        if (request.ThemeMode != null) preference.CustomAccentColor = request.ThemeMode; // Reusing AccentColor for Mode

        preference.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}

// Endpoint
public static class UpdatePreferencesEndpoint
{
    public static void MapUpdatePreferences(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/preferences", async (
            UpdatePreferencesRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var command = new UpdatePreferencesCommand(
                userId,
                request.ThemeColor,
                request.ThemeMode
            );

            var result = await mediator.Send(command, ct);
            return result ? Results.Ok(new { Success = true }) : Results.BadRequest();
        })
        .RequireAuthorization()
        .WithTags("Preferences");
    }
}

// DTO
public record UpdatePreferencesRequest(
    string? ThemeColor,
    string? ThemeMode
);
