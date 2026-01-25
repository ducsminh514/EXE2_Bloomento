using FluentValidation;

namespace ADHDChecklist.API.Features.Tasks.UpdateTask
{
    public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
    {
        public UpdateTaskCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề là bắt buộc")
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Priority)
                .InclusiveBetween(1, 3);

            RuleFor(x => x.TimeBlockEnd)
                .GreaterThan(x => x.TimeBlockStart)
                .When(x => x.TimeBlockStart.HasValue && x.TimeBlockEnd.HasValue);
        }
    }
}
