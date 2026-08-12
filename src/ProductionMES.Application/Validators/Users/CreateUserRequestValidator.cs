using FluentValidation;
using ProductionMES.Application.DTOs.Users;

namespace ProductionMES.Application.Validators.Users;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
            .MaximumLength(100).WithMessage("Tên đăng nhập tối đa 100 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MaximumLength(200).WithMessage("Mật khẩu tối đa 200 ký tự.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(200).WithMessage("Họ tên tối đa 200 ký tự.");

        RuleFor(x => x.UserRole)
            .IsInEnum().WithMessage("Vai trò không hợp lệ.");
    }
}
