namespace ADHDChecklist.API.Features.Categories
{
    public record CategoryResponse(
      Guid Id,
      string Name,
      string ColorHex,
      string? Icon,
      int OrderIndex,
      int TaskCount
  );

    public record CreateCategoryRequest(
        string Name,
        string ColorHex,
        string? Icon
    );

    public record UpdateCategoryRequest(
        string Name,
        string ColorHex,
        string? Icon
    );
}
