using ADHDChecklist.API.Shared.Events;
using MediatR;

namespace ADHDChecklist.API.Features.Pets.EventHandlers;

public class PetExperienceEventHandler : 
    INotificationHandler<TaskCompletedEvent>,
    INotificationHandler<HabitCompletedEvent>
{
    private readonly IPetService _petService;
    private readonly ILogger<PetExperienceEventHandler> _logger;

    public PetExperienceEventHandler(IPetService petService, ILogger<PetExperienceEventHandler> logger)
    {
        _petService = petService;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
    {
        // Cho phép xử lý cả điểm âm để đồng bộ khi Task bị Un-check (Sửa lỗi XP Exploit)
        _logger.LogInformation("Processing Pet XP for Task {TaskId}. Points: {Points}", notification.TaskId, notification.Points);
        
        await _petService.AddXpAsync(notification.UserId, notification.Points);
    }

    public async System.Threading.Tasks.Task Handle(HabitCompletedEvent notification, CancellationToken cancellationToken)
    {
        // Tương tự Task, Habit cũng cần tính đúng điểm tăng/giảm
        _logger.LogInformation("Processing Pet XP for Habit {HabitId}. Points: {Points}", notification.HabitId, notification.Points);
        
        await _petService.AddXpAsync(notification.UserId, notification.Points);
    }
}
