using FluentValidation;
using ProductionMES.Application.DTOs.Stages;

namespace ProductionMES.Application.Validators.Stages;

public class CreateStageRequestValidator : AbstractValidator<CreateStageRequest>
{
    public CreateStageRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên công đoạn không được để trống.")
            .MaximumLength(200).WithMessage("Tên công đoạn tối đa 200 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");
    }
}
