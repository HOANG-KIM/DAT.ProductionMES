using FluentValidation;
using ProductionMES.Application.DTOs.Users;

namespace ProductionMES.Application.Validators.Users;

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.UserRole)
            .IsInEnum().WithMessage("Vai trò không hợp lệ.");
    }
}
