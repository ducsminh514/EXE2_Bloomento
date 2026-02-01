using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities
{
    public class ArticleBookmark : BaseEntity, IAuditable
    {
        public Guid ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
