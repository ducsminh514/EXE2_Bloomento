using MediatR;

namespace ADHDChecklist.API.Features.Auth.Login
{
    // ============================================
    // ENDPOINT
    // ============================================
    public static class LoginEndpoint
    {
        public static RouteHandlerBuilder MapLogin(this IEndpointRouteBuilder app)
        {
            return app.MapPost("/api/auth/login", async (
                LoginCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);

                return result.Success
                    ? Results.Ok(result)
                    : Results.Unauthorized();
            })
            .WithTags("Authentication")
            .WithName("Login")
            .Produces<LoginResponse>(200)
            .Produces(401);
        }
    }
}
