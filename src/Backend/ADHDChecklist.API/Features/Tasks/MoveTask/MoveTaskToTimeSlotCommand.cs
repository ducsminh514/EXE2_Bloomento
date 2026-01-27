
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.MoveTask
{
    public record MoveTaskToTimeSlotCommand(
       Guid TaskId,
       TimeOnly TimeBlockStart,
       TimeOnly TimeBlockEnd,
       Guid UserId
   ) : IRequest<bool>;
    public record MoveToTimeSlotRequest(string TimeBlockStart, string TimeBlockEnd);

}
