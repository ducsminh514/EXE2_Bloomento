using MediatR;

namespace ADHDChecklist.API.Features.Auth.VerifyEmail
{
    // ============================================
    // ENDPOINT
    // ============================================
    public static class VerifyEmailEndpoint
    {
        public static void MapVerifyEmail(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/verify-email", async (
                VerifyEmailCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithTags("Authentication")
            .WithName("VerifyEmail")
            .Produces<VerifyEmailResponse>(200)
            .Produces<VerifyEmailResponse>(400);
        }
    }
}
