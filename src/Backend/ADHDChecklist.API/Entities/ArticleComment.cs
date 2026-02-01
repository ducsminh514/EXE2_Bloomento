using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities
{
    public class ArticleComment : BaseEntity, IAuditable
    {
        public Guid ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        
        public Guid? ParentCommentId { get; set; }
        public ArticleComment? ParentComment { get; set; }
        public ICollection<ArticleComment> Replies { get; set; } = new List<ArticleComment>();

        public bool IsHidden { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
