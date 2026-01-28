using MediatR;
using System;

namespace ADHDChecklist.API.Features.Analytics.Premium
{
    public record GetPremiumAnalyticsQuery(Guid UserId) : IRequest<PremiumAnalyticsResponse>;
}
