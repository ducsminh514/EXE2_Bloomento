using FluentValidation;

namespace ADHDChecklist.API.Features.Auth.Register
{
    // ============================================
    // VALIDATOR
    // ============================================
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email là bắt buộc")
                .EmailAddress().WithMessage("Email không hợp lệ")
                .MaximumLength(256);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu là bắt buộc")
                .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự")
                .Matches(@"[A-Z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ hoa")
                .Matches(@"[a-z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ thường")
                .Matches(@"[0-9]").WithMessage("Mật khẩu phải có ít nhất 1 số")
                .Matches(@"[\W_]").WithMessage("Mật khẩu phải có ít nhất 1 ký tự đặc biệt");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên là bắt buộc")
                .MaximumLength(100);
        }
    }
}
