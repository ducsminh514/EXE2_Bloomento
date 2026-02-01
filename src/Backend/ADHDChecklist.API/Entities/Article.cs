using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities
{
    public class Article : BaseEntity, IAuditable
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        
        public Guid? CategoryId { get; set; }
        public KnowledgeCategory? Category { get; set; }

        public Guid AuthorId { get; set; }
        public ApplicationUser Author { get; set; } = null!;

        public int EstimatedReadTimeMinutes { get; set; } = 5;
        public string Difficulty { get; set; } = "Easy"; // Easy, Medium, Hard
        public bool IsPremium { get; set; } = false;

        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int BookmarkCount { get; set; }

        public DateTime? PublishedAt { get; set; }
        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<ArticleComment> Comments { get; set; } = new List<ArticleComment>();
        public ICollection<ArticleBookmark> Bookmarks { get; set; } = new List<ArticleBookmark>();
    }
}
