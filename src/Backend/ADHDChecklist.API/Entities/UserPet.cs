using System;
using System.ComponentModel.DataAnnotations;
using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Entities.Enums;

namespace ADHDChecklist.API.Entities;

/// <summary>
/// Linh hồn: Lưu trữ dữ liệu động của con pet thuộc sở hữu của User.
/// </summary>
public class UserPet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TemplateId { get; set; }
    
    public string CustomName { get; set; } = null!;
    
    // Denormalization: Lưu cả XP và Level để tối ưu tốc độ đọc
    public int TotalXp { get; set; } = 0;
    public int CurrentLevel { get; set; } = 1;

    public int CurrentHealth { get; set; } = 100;
    
    /// <summary>
    /// Ngày cuối cùng thực hiện hành động (Task/Habit) để hiển thị trên UI hoặc tính logic khác.
    /// </summary>
    public DateTime LastActionAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày cuối cùng tính toán HP (Hunger timer). Chỉ reset khi HP thực sự giảm.
    /// </summary>
    public DateTime LastHpUpdateAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Trạng thái: Healthy, Sick, Hibernate
    /// </summary>
    public PetState State { get; set; } = PetState.Healthy;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Concurrency Token: Managed as a regular column for Postgres compatibility
    public byte[] RowVersion { get; set; } = new byte[8];

    // Navigation
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual PetTemplate Template { get; set; } = null!;
}
