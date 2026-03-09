using System.Security.Claims;
using ADHDChecklist.API.Features.Pets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ADHDChecklist.API.Features.Pets;

public static class PetEndpoints
{
    public static void MapPetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pets")
            .RequireAuthorization()
            .WithTags("Pets");

        // Lấy thông tin Pet hiện tại
        group.MapGet("/my-pet", async (
            ClaimsPrincipal user,
            IPetService petService) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await petService.GetUserPetAsync(userId);
            return Results.Ok(result);
        })
        .WithName("GetMyPet");

        // Cập nhật tên Pet
        group.MapPatch("/my-pet/name", async (
            UpdatePetNameRequest request,
            ClaimsPrincipal user,
            IPetService petService) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await petService.UpdatePetNameAsync(userId, request.NewName);
            return Results.NoContent();
        })
        .WithName("UpdatePetName");

        // Sử dụng vật phẩm hồi phục
        group.MapPost("/my-pet/recover", async (
            ClaimsPrincipal user,
            IPetService petService) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await petService.UseRecoveryItemAsync(userId);
            return Results.NoContent();
        })
        .WithName("RecoverPet");
    }
}

public record UpdatePetNameRequest(string NewName);
