using FluentValidation;
using ProductionMES.Application.DTOs.Lines;

namespace ProductionMES.Application.Validators.Lines;

public class CreateLineRequestValidator : AbstractValidator<CreateLineRequest>
{
    public CreateLineRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên Line không được để trống.")
            .MaximumLength(200).WithMessage("Tên Line tối đa 200 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");
    }
}
