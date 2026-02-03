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
    string? RecurrencePattern,
    string? DopamineType = "Low",
    Guid? FamilyId = null,
    Guid? AssignedUserId = null,
    bool IsShared = false
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
    bool IsCompleted,
    string? DopamineType = null,
    Guid? AssignedUserId = null,
    bool IsShared = false
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
    DateTime UpdatedAt,
    int? RescheduleCount,
    string? DopamineType,
    Guid? FamilyId,
    Guid? AssignedUserId,
    string? AssignedUserName,
    string? AssignedUserAvatar,
    string? AssignedUserColor,
    bool IsShared,
    string? AssignmentStatus,
    string? RejectionReason
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