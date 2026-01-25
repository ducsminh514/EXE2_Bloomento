using ADHDChecklist.API.Shared.DTOs;
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.GetTasksByDate
{
    public record GetTasksByDateQuery(
     DateOnly Date,
     Guid UserId
 ) : IRequest<TaskListResponse>;
}
