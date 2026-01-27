using MediatR;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Features.Users.Upgrade;

public record UpgradeCommand(
    string UserId,
    SubscriptionTier Tier
) : IRequest<UpgradeResponse>;

public record UpgradeResponse(
    bool Success,
    string Message,
    DateTime? ExpiryDate
);
