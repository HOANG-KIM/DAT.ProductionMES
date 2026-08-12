using FluentValidation;
using ProductionMES.Application.DTOs.ProductionPlans;

namespace ProductionMES.Application.Validators.ProductionPlans;

public class UpdateProductionPlanRequestValidator : AbstractValidator<UpdateProductionPlanRequest>
{
    public UpdateProductionPlanRequestValidator()
    {
        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("Mã sản phẩm không được để trống.")
            .MaximumLength(100).WithMessage("Mã sản phẩm tối đa 100 ký tự.");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(200).WithMessage("Tên sản phẩm tối đa 200 ký tự.");

        RuleFor(x => x.PlannedQuantity)
            .GreaterThan(0).WithMessage("Số lượng kế hoạch phải lớn hơn 0.");

        RuleFor(x => x.TaktTimeSeconds)
            .GreaterThan(0).WithMessage("Takt time phải lớn hơn 0.");

        RuleFor(x => x.Shift)
            .NotEmpty().WithMessage("Ca làm việc không được để trống.")
            .MaximumLength(50).WithMessage("Ca làm việc tối đa 50 ký tự.");

        RuleFor(x => x.EffectiveDate)
            .NotEqual(default(DateTime)).WithMessage("Ngày áp dụng không được để trống.");
    }
}
