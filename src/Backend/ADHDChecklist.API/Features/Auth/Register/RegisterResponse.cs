namespace ADHDChecklist.API.Features.Auth.Register
{
    public record RegisterResponse(
       bool Success,
       string Message,
       string? UserId = null
   );
}
