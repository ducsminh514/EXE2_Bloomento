using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Entities.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using System ;

namespace ADHDChecklist.API.Features.Payments.CreatePayment;

public record CreatePaymentRequest(SubscriptionTier Tier);
public record CreatePaymentResponse(string CheckoutUrl, long OrderCode);

public record CreatePaymentCommand(SubscriptionTier Tier, Guid UserId) : IRequest<CreatePaymentResponse?>;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResponse?>
{
    private readonly AppDbContext _context;
    private readonly PayOSClient _payOS;
    private readonly IConfiguration _configuration;

    public CreatePaymentCommandHandler(AppDbContext context, PayOSClient payOS, IConfiguration configuration)
    {
        _context = context;
        _payOS = payOS;
        _configuration = configuration;
    }

    public async Task<CreatePaymentResponse?> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) return null;

            int amount = request.Tier switch
            {
                SubscriptionTier.Premium => 49000,
                SubscriptionTier.Family => 99000,
                _ => 0
            };

            if (amount == 0) return null;

            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyMMddHHmmssfff"));
            
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                OrderCode = orderCode,
                Amount = amount,
                Description = $"Nâng cấp gói {request.Tier}",
                UserId = request.UserId,
                SubscriptionTier = request.Tier,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            string domain = _configuration["FrontendUrl"] ?? "http://localhost:7002";
            
            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = transaction.Description.Length > 25 ? transaction.Description.Substring(0, 25) : transaction.Description,
                CancelUrl = $"{domain}/payment/cancel",
                ReturnUrl = $"{domain}/payment/success"
            };

            var result = await _payOS.PaymentRequests.CreateAsync(paymentRequest);
            
            transaction.PaymentLinkId = result.PaymentLinkId;
            await _context.SaveChangesAsync(cancellationToken);

            return new CreatePaymentResponse(result.CheckoutUrl, orderCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Payment Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            // Return null so the endpoint returns 400 instead of 500
            return null; 
        }
    }
}

public static class CreatePaymentEndpoint
{
    public static void MapCreatePayment(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments/create", async (
            CreatePaymentRequest request,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Results.Unauthorized();
            
            var userId = Guid.Parse(userIdStr);
            var command = new CreatePaymentCommand(request.Tier, userId);
            
            var result = await mediator.Send(command);
            
            return result != null 
                ? Results.Ok(result) 
                : Results.BadRequest("Yêu cầu không hợp lệ hoặc không tìm thấy người dùng");
        })
        .RequireAuthorization()
        .WithTags("Payments")
        .WithName("CreatePayment");
    }
}
