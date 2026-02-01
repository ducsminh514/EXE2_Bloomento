using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities
{
    public class ReadingProgress : BaseEntity, IAuditable
    {
        public Guid ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public int ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime LastReadAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
