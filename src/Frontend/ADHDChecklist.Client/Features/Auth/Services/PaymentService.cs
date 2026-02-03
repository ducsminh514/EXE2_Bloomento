using ADHDChecklist.Client.Infrastructure.Services;
using ADHDChecklist.Client.Shared.Models;

namespace ADHDChecklist.Client.Features.Auth.Services;

public interface IPaymentService
{
    Task<ADHDChecklist.Client.Shared.Models.CreatePaymentResponse?> CreatePaymentLinkAsync(ADHDChecklist.Client.Shared.Models.CreatePaymentRequest request);
}

public class PaymentService : IPaymentService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IApiClient apiClient, ILogger<PaymentService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<ADHDChecklist.Client.Shared.Models.CreatePaymentResponse?> CreatePaymentLinkAsync(ADHDChecklist.Client.Shared.Models.CreatePaymentRequest request)
    {
        try
        {
            return await _apiClient.PostAsync<ADHDChecklist.Client.Shared.Models.CreatePaymentResponse>(
                "/api/payments/create",
                request
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment link");
            return null;
        }
    }
}
