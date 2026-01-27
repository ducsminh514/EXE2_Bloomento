using MediatR;
using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Shared.DTOs;
namespace ADHDChecklist.API.Features.Tasks.GetTaskById
{
    public record GetTaskByIdQuery(
        Guid TaskId,
        Guid UserId
    ) : IRequest<TaskResponse?>;
}
