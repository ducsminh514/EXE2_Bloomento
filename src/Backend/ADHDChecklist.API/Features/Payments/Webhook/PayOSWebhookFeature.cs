using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace ADHDChecklist.API.Features.Payments.Webhook;

public static class PayOSWebhookFeature
{
    
    public static void MapPayOSWebhook(this IEndpointRouteBuilder app)
    {
        // Accept CORS preflight request from PayOS Dashboard
        app.MapMethods("/api/payments/webhook", new[] { "OPTIONS" }, () => Results.Ok())
           .ExcludeFromDescription();

        // Accept a dummy GET request in case PayOS pings it to verify URL existence
        app.MapGet("/api/payments/webhook", () => Results.Ok(new { success = true, message = "Webhook endpoint is active." }))
           .ExcludeFromDescription();

        app.MapPost("/api/payments/webhook", async (
            [Microsoft.AspNetCore.Mvc.FromBody] PayOS.Models.Webhooks.Webhook webhookBody,
            HttpContext context,
            AppDbContext dbContext,
            PayOSClient payOS,
            ILoggerFactory loggerFactory,
            UserManager<ApplicationUser> userManager) =>
        {
            var logger = loggerFactory.CreateLogger("PayOSWebhook");
            try
            {
                logger.LogInformation("Received webhook: {Webhook}", 
                    System.Text.Json.JsonSerializer.Serialize(webhookBody));
                // 1. Handle PayOS Test/Connect Payload (No Signature Case)
                // PayOS usually sends a test payload when you save the URL. 
                // We assume if OrderCode is 0 or data is null, it's a test.
                if (webhookBody?.Data == null || webhookBody.Data.OrderCode == 0)
                {
                    logger.LogInformation("Test webhook detected - Responding 200 OK");
                    return Results.Ok(new { success = true, message = "Webhook received" });
                }

                // 2. Real Webhook - Strict Verification
                
                    // Strict Verification
                    var result = await payOS.Webhooks.VerifyAsync(webhookBody);
                    if (result == null)
                    {
                        // Log but try to see if it's a test ping
                        logger.LogWarning("Webhook Signature Verification Failed on payload: {Data}", System.Text.Json.JsonSerializer.Serialize(webhookBody));
                        
                        // If PayOS dashboard sends a test ping that fails signature, we might need to return 200 OK just to let it save.
                        // But usually, PayOS DOES send a valid signature even for test.
                        // If result is null, it means signature mismatch.
                        return Results.BadRequest("Invalid signature");
                    }
                    var verifiedData = result;
                    var data = verifiedData; 
                    
                    // Proceed with normal logic
                    logger.LogInformation("Received Webhook for Order {OrderCode}, Code {Code}", data.OrderCode, data.Code);

                    if (data.Code == "00")
                    {
                        var transaction = await dbContext.Transactions
                            .FirstOrDefaultAsync(t => t.OrderCode == data.OrderCode);

                    if (transaction != null && transaction.Status == "PENDING")
                    {
                        transaction.Status = "PAID";
                        transaction.UpdatedAt = DateTime.UtcNow;

                        var user = await userManager.FindByIdAsync(transaction.UserId.ToString());
                        if (user != null)
                        {
                            user.PreviousSubscriptionTier = user.SubscriptionTier; // Save previous tier
                            user.SubscriptionTier = transaction.SubscriptionTier;
                            user.SubscriptionExpiry = DateTime.UtcNow.AddDays(30);
                            await userManager.UpdateAsync(user);
                            
                            logger.LogInformation("User {UserId} upgraded to {Tier} via Webhook", 
                                                   user.Id, transaction.SubscriptionTier);
                        }

                        await dbContext.SaveChangesAsync();
                    }
                }
                else if (data.Code == "01" || data.Code == "CANCELLED")
                {
                    var transaction = await dbContext.Transactions
                        .FirstOrDefaultAsync(t => t.OrderCode == data.OrderCode);
                    
                    if (transaction != null && transaction.Status == "PENDING")
                    {
                        transaction.Status = "CANCELLED";
                        transaction.UpdatedAt = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                    }
                }

                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing PayOS Webhook");
                return Results.Problem("Webhook processing failed", statusCode: 500);
            }
        })
        .WithTags("Payments")
        .WithName("PayOSWebhook");
    }
}

