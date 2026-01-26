using MediatR;

namespace ADHDChecklist.API.Features.Analytics.FreeTier
{
    public record GetWeeklyAnalyticsQuery(Guid UserId) : IRequest<WeeklyAnalyticsResponse>;

}
