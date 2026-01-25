using MediatR;

namespace ADHDChecklist.API.Features.Auth.Register
{
    // ============================================
    // ENDPOINT
    // ============================================
    public static class RegisterEndpoint
    {
        public static void MapRegister(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/register", async (
                RegisterCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithTags("Authentication")
            .WithName("Register")
            .Produces<RegisterResponse>(200)
            .Produces<RegisterResponse>(400);
        }
    }
}
