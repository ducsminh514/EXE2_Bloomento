using FluentValidation;

namespace ADHDChecklist.API.Features.Tasks.CreateTask
{
    public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề là bắt buộc")
                .MaximumLength(200).WithMessage("Tiêu đề không được quá 200 ký tự");

            RuleFor(x => x.Description)
                .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Mô tả không được quá 1000 ký tự");

            RuleFor(x => x.Priority)
                .InclusiveBetween(1, 3).WithMessage("Priority phải từ 1-3");

            RuleFor(x => x.TimeBlockEnd)
                .GreaterThan(x => x.TimeBlockStart)
                .When(x => x.TimeBlockStart.HasValue && x.TimeBlockEnd.HasValue)
                .WithMessage("Giờ kết thúc phải sau giờ bắt đầu");

            RuleFor(x => x.RecurrencePattern)
                .NotEmpty()
                .When(x => x.IsRecurring)
                .WithMessage("Pattern lặp lại là bắt buộc khi task recurring");
        }
    }
}
