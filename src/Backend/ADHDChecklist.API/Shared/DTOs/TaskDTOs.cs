namespace ADHDChecklist.API.Shared.DTOs;

// ============================================
// TASK REQUEST DTOs
// ============================================
public record CreateTaskRequest(
    string Title,
    string? Description,
    Guid? CategoryId,
    DateOnly ScheduledDate,
    TimeOnly? TimeBlockStart,
    TimeOnly? TimeBlockEnd,
    int? Duration,
    int Priority,
    bool IsRecurring,
    string? RecurrencePattern
);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    Guid? CategoryId,
    DateOnly ScheduledDate,
    TimeOnly? TimeBlockStart,
    TimeOnly? TimeBlockEnd,
    int? Duration,
    int Priority,
    bool IsCompleted
);

public record MoveTaskRequest(
    DateOnly NewScheduledDate,
    TimeOnly? NewTimeBlockStart,
    TimeOnly? NewTimeBlockEnd
);

// ============================================
// TASK RESPONSE DTOs
// ============================================
public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryColor,
    DateOnly ScheduledDate,
    TimeOnly? TimeBlockStart,
    TimeOnly? TimeBlockEnd,
    int? Duration,
    bool? IsCompleted,
    DateTime? CompletedAt,
    int Priority,
    bool? IsRecurring,
    string? RecurrencePattern,
    Guid? ParentTaskId,
    List<TaskResponse> SubTasks,
    int? OrderIndex,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TaskListResponse(
    List<TaskResponse> Tasks,
    int TotalCount,
    DateOnly Date
);

public record TaskStatisticsResponse(
    int TotalTasks,
    int CompletedTasks,
    int PendingTasks,
    int OverdueTasks,
    double CompletionRate
);