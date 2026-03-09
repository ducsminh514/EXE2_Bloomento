using MediatR;

namespace ADHDChecklist.API.Features.Auth.Logout
{
    public static class LogoutEndpoint
    {
        public static RouteHandlerBuilder MapLogout(this IEndpointRouteBuilder app)
        {
            return app.MapPost("/api/auth/logout", async (
                LogoutCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);
                return Results.Ok(result);
            })
            .WithTags("Authentication")
            .WithName("Logout")
            .Produces<LogoutResponse>(200);
        }
    }
}
