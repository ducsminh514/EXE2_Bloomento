using System;
using System.Collections.Generic;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities;

public partial class Task
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }

    public DateOnly ScheduledDate { get; set; }

    public TimeOnly? TimeBlockStart { get; set; }

    public TimeOnly? TimeBlockEnd { get; set; }

    public int? Duration { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? Priority { get; set; }

    public bool? IsRecurring { get; set; }

    public string? RecurrencePattern { get; set; }

    public Guid? ParentTaskId { get; set; }

    public int? OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int RescheduleCount { get; set; } = 0;
    public string? DopamineType { get; set; } // 'Low' (Hard/Boring) or 'High' (Fun/Creative)

    // Family Features (Nullable for backward compatibility)
    public Guid? FamilyId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public bool IsShared { get; set; } = false;
    
    // Assignment Status: "Pending", "Accepted", "Rejected"
    public string? AssignmentStatus { get; set; } = "Pending"; 
    public string? RejectionReason { get; set; }
    public bool IsMandatory { get; set; } = false; // "Bắt buộc làm" - Cannot be rejected

    // Completion Approval: "None", "Pending", "Approved", "ReworkRequested"
    public string CompletionApprovalStatus { get; set; } = "None";
    public Guid? ApproverUserId { get; set; }

    public virtual ICollection<BrainDumpItem> BrainDumpItems { get; set; } = new List<BrainDumpItem>();

    public virtual Category? Category { get; set; }
    
    public virtual ICollection<FocusSession> FocusSessions { get; set; } = new List<FocusSession>();

    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Task? ParentTask { get; set; }
    public virtual ICollection<Task> SubTasks { get; set; } = new List<Task>();
    
    // Family Navigation
    public virtual Family? Family { get; set; }
    public virtual ApplicationUser? AssignedUser { get; set; }
}
