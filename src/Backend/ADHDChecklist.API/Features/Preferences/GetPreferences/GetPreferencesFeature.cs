using System.Security.Claims;
using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Preferences.GetPreferences;

// Query
public record GetPreferencesQuery(Guid UserId) : IRequest<PreferencesResponse?>;

// Response DTO
public record PreferencesResponse(
    string? ThemeColor,
    string? ThemeMode
);

// Handler
public class GetPreferencesHandler : IRequestHandler<GetPreferencesQuery, PreferencesResponse?>
{
    private readonly AppDbContext _context;

    public GetPreferencesHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PreferencesResponse?> Handle(GetPreferencesQuery request, CancellationToken ct)
    {
        var preference = await _context.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        if (preference == null) return null;

        return new PreferencesResponse(
            preference.CustomPrimaryColor ?? "indigo",
            preference.CustomAccentColor ?? "light"
        );
    }
}

// Endpoint
public static class GetPreferencesEndpoint
{
    public static void MapGetPreferences(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/preferences", async (
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await mediator.Send(new GetPreferencesQuery(userId), ct);
            
            // Return default if not found
            return Results.Ok(result ?? new PreferencesResponse("indigo", "light"));
        })
        .RequireAuthorization()
        .WithTags("Preferences");
    }
}
