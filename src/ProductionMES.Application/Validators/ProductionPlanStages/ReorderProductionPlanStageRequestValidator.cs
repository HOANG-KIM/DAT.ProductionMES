using FluentValidation;
using ProductionMES.Application.DTOs.ProductionPlanStages;

namespace ProductionMES.Application.Validators.ProductionPlanStages;

public class ReorderProductionPlanStageRequestValidator : AbstractValidator<ReorderProductionPlanStageRequest>
{
    public ReorderProductionPlanStageRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Danh sách trình tự công đoạn không được để trống.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StageId).GreaterThan(0).WithMessage("Phải chọn 1 công đoạn hợp lệ.");
            item.RuleFor(i => i.SequenceNumber).GreaterThan(0).WithMessage("Số thứ tự phải lớn hơn 0.");
        });
    }
}
