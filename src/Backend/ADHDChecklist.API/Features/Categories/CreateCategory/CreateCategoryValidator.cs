using FluentValidation;

namespace ADHDChecklist.API.Features.Categories.CreateCategory
{
    public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên category là bắt buộc")
                .MaximumLength(50);

            RuleFor(x => x.ColorHex)
                .Matches("^#[0-9A-Fa-f]{6}$")
                .WithMessage("Màu phải ở định dạng hex (ví dụ: #FF5733)");
        }
    }
}
