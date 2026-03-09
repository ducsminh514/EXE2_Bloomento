using MediatR;

namespace ADHDChecklist.API.Features.Auth.GoogleLogin
{
    // ============================================
    // ENDPOINT
    // ============================================
    public static class GoogleLoginEndpoint
    {
        public static RouteHandlerBuilder MapGoogleLogin(this IEndpointRouteBuilder app)
        {
            return app.MapPost("/api/auth/google-login", async (
                GoogleLoginCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithTags("Authentication")
            .WithName("GoogleLogin")
            .Produces<GoogleLoginResponse>(200)
            .Produces<GoogleLoginResponse>(400);
        }
    }
}
