using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace ADHDChecklist.API.Features.BrainDump.CreateBrainDumpItem;

// Command
public record CreateBrainDumpItemCommand(
    Guid UserId,
    string Content
) : IRequest<BrainDumpItemResponse>;

// Response
public record BrainDumpItemResponse(
    Guid Id,
    string Content,
    DateTime CreatedAt
);

// Validator
public class CreateBrainDumpItemValidator : AbstractValidator<CreateBrainDumpItemCommand>
{
    public CreateBrainDumpItemValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(500);
    }
}

// Handler
public class CreateBrainDumpItemHandler : IRequestHandler<CreateBrainDumpItemCommand, BrainDumpItemResponse>
{
    private readonly AppDbContext _context;

    public CreateBrainDumpItemHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BrainDumpItemResponse> Handle(CreateBrainDumpItemCommand request, CancellationToken ct)
    {
        var item = new BrainDumpItem
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _context.BrainDumpItems.Add(item);
        await _context.SaveChangesAsync(ct);

        return new BrainDumpItemResponse(item.Id, item.Content, item.CreatedAt);
    }
}

// Endpoint
public static class CreateBrainDumpItemEndpoint
{
    public static void MapCreateBrainDumpItem(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/braindump", async (
            CreateBrainDumpItemRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var command = new CreateBrainDumpItemCommand(userId, request.Content);
            var result = await mediator.Send(command, ct);
            return Results.Ok(new { Success = true, Data = result });
        })
        .RequireAuthorization()
        .WithTags("BrainDump");
    }
}

// Request DTO
public record CreateBrainDumpItemRequest(string Content);
