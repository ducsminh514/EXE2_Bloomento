using MediatR;

namespace ADHDChecklist.API.Features.Auth.ResendVerification
{
    public static class ResendVerificationEndpoint
    {
        public static RouteHandlerBuilder MapResendVerification(this IEndpointRouteBuilder app)
        {
            return app.MapPost("/api/auth/resend-verification", async (
                ResendVerificationCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithTags("Authentication")
            .WithName("ResendVerification")
            .Produces<ResendVerificationResponse>(200)
            .Produces<ResendVerificationResponse>(400);
        }
    }
}
