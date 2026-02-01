using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities
{
    public class KnowledgeCategory : BaseEntity, IAuditable
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string ColorHex { get; set; } = "#3B82F6";
        public Guid? ParentCategoryId { get; set; }
        public int OrderIndex { get; set; }

        public KnowledgeCategory? ParentCategory { get; set; }
        public ICollection<KnowledgeCategory> SubCategories { get; set; } = new List<KnowledgeCategory>();
        public ICollection<Article> Articles { get; set; } = new List<Article>();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
