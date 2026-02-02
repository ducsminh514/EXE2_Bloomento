using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace ADHDChecklist.API.Features.Common.Upload
{
    // Command
    public record UploadImageCommand(IFormFile File) : IRequest<string>;

    // Handler
    public class UploadImageHandler : IRequestHandler<UploadImageCommand, string>
    {
        private readonly IWebHostEnvironment _environment;

        public UploadImageHandler(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> Handle(UploadImageCommand request, CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
                throw new ArgumentException("No file uploaded");

            // Simple validation
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".tiff", ".tif" };
            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException($"Invalid file type: {extension} (Allowed: {string.Join(", ", allowedExtensions)})");

            // Ensure directory exists
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }
            
            var uploadPath = Path.Combine(webRootPath, "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream, cancellationToken);
            }

            // Return relative URL
            return $"/uploads/{fileName}";
        }
    }

    // Endpoint
    public static class UploadImageEndpoint
    {
        public static void MapUploadImageEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/upload/image", async (IFormFile file, IMediator mediator) =>
            {
                try
                {
                    var url = await mediator.Send(new UploadImageCommand(file));
                    return Results.Ok(new { Url = url });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
            })
            .WithTags("Common")
            .RequireAuthorization("AdminPolicy") // Only admin can upload for now
            .DisableAntiforgery(); // Often needed for file uploads in some setups, or strictly required headers
        }
    }
}
