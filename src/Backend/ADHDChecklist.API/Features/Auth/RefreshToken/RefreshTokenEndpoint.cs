using MediatR;

namespace ADHDChecklist.API.Features.Auth.RefreshToken
{
    public static class RefreshTokenEndpoint
    {
        public static RouteHandlerBuilder MapRefreshToken(this IEndpointRouteBuilder app)
        {
            return app.MapPost("/api/auth/refresh-token", async (
                RefreshTokenCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);

                return result.Success
                    ? Results.Ok(result)
                    : Results.Unauthorized();
            })
            .WithTags("Authentication")
            .WithName("RefreshToken")
            .Produces<RefreshTokenResponse>(200)
            .Produces(401);
        }
    }
}
