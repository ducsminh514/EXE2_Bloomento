using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ADHDChecklist.API.Features.Knowledge.Categories
{
    // Response DTO
    public record KnowledgeCategoryResponse(Guid Id, string Name, string Slug, string? Description, string? Icon, string ColorHex, int OrderIndex);

    // Query
    public record GetCategoriesQuery : IRequest<List<KnowledgeCategoryResponse>>;

    // Handler
    public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, List<KnowledgeCategoryResponse>>
    {
        private readonly AppDbContext _context;

        public GetCategoriesHandler(AppDbContext context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task<List<KnowledgeCategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            return await _context.KnowledgeCategories
                .OrderBy(c => c.OrderIndex)
                .Select(c => new KnowledgeCategoryResponse(c.Id, c.Name, c.Slug, c.Description, c.Icon, c.ColorHex, c.OrderIndex))
                .ToListAsync(cancellationToken);
        }
    }

    // Endpoint
    public static class GetCategoriesEndpoint
    {
        public static void MapGetKnowledgeCategories(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/knowledge/categories", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetCategoriesQuery());
                return Results.Ok(result);
            })
            .WithTags("Knowledge")
            .WithName("GetKnowledgeCategories")
            .AllowAnonymous(); // Knowledge should be public? Or authorized? Let's allow anonymous for now for Preview.
        }
    }
}
