using ADHDChecklist.API.Entities.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ADHDChecklist.API.Features.Users.Upgrade;

public class UpgradeCommandHandler : IRequestHandler<UpgradeCommand, UpgradeResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UpgradeCommandHandler> _logger;

    public UpgradeCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<UpgradeCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UpgradeResponse> Handle(UpgradeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return new UpgradeResponse(false, "Không tìm thấy người dùng", null);
            }

            // Upgrade Logic (Mock Payment Success)
            user.SubscriptionTier = request.Tier;
            
            // If not Free, set expiry to 30 days from now
            if (request.Tier != SubscriptionTier.Free)
            {
                user.SubscriptionExpiry = DateTime.UtcNow.AddDays(30);
                _logger.LogInformation("User {UserId} upgraded to {Tier} until {Expiry}", request.UserId, request.Tier, user.SubscriptionExpiry);
            }
            else 
            {
                user.SubscriptionExpiry = null;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return new UpgradeResponse(true, "Nâng cấp thành công!", user.SubscriptionExpiry);
            }

            return new UpgradeResponse(false, "Lỗi cập nhật dữ liệu", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading user {UserId}", request.UserId);
            return new UpgradeResponse(false, "Có lỗi xảy ra", null);
        }
    }
}
