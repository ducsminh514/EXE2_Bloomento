using System.ComponentModel.DataAnnotations;

namespace ADHDChecklist.Client.Features.Knowledge.DTOs;

public class ArticleEditorDto
{
    public Guid Id { get; set; } // For Edit

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug là bắt buộc")]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Slug chỉ chứa chữ thường, số và dấu gạch ngang")]
    public string Slug { get; set; } = string.Empty;

    public string? Summary { get; set; }

    [Required(ErrorMessage = "Nội dung không được để trống")]
    public string Content { get; set; } = string.Empty;

    public string? CoverImage { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public Guid CategoryId { get; set; }

    public int EstimatedReadTimeMinutes { get; set; } = 5;

    public string Difficulty { get; set; } = "Easy";

    public bool IsPublished { get; set; } = false;
    public bool IsPremium { get; set; } = false;
}
