using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ADHDChecklist.API.Features.Categories;
namespace ADHDChecklist.API.Features.Categories
{
    public static class CategoryEndpoints
    {
        public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
        {
            // GET all categories
            app.MapGet("/api/categories", async (
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var query = new GetCategoriesQuery(userId);
                var result = await mediator.Send(query, ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .WithTags("Categories")
            .WithName("GetCategories");

            // CREATE category
            app.MapPost("/api/categories", async (
                CreateCategoryRequest request,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var command = new CreateCategoryCommand(
                    request.Name,
                    request.ColorHex,
                    request.Icon,
                    userId
                );
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/categories/{result.Id}", result);
            })
            .RequireAuthorization()
            .WithTags("Categories")
            .WithName("CreateCategory");

            // UPDATE category
            app.MapPut("/api/categories/{id:guid}", async (
                Guid id,
                UpdateCategoryRequest request,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var command = new UpdateCategoryCommand(
                    id,
                    request.Name,
                    request.ColorHex,
                    request.Icon,
                    userId
                );
                var result = await mediator.Send(command, ct);
                return result != null ? Results.Ok(result) : Results.NotFound();
            })
            .RequireAuthorization()
            .WithTags("Categories")
            .WithName("UpdateCategory");

            // DELETE category
            app.MapDelete("/api/categories/{id:guid}", async (
                Guid id,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var command = new DeleteCategoryCommand(id, userId);
                var result = await mediator.Send(command, ct);
                return result ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization()
            .WithTags("Categories")
            .WithName("DeleteCategory");
        }
    }
}
