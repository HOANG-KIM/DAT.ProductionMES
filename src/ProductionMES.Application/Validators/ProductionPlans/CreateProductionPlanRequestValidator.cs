using FluentValidation;
using ProductionMES.Application.DTOs.ProductionPlans;

namespace ProductionMES.Application.Validators.ProductionPlans;

public class CreateProductionPlanRequestValidator : AbstractValidator<CreateProductionPlanRequest>
{
    public CreateProductionPlanRequestValidator()
    {
        RuleFor(x => x.LineId)
            .GreaterThan(0).WithMessage("Phải chọn 1 Line hợp lệ.");

        RuleFor(x => x.Customer)
            .NotEmpty().WithMessage("Khách hàng không được để trống.")
            .MaximumLength(200).WithMessage("Khách hàng tối đa 200 ký tự.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model không được để trống.")
            .MaximumLength(200).WithMessage("Model tối đa 200 ký tự.");

        RuleFor(x => x.Lot)
            .NotEmpty().WithMessage("Lot không được để trống.")
            .MaximumLength(100).WithMessage("Lot tối đa 100 ký tự.");

        // AC2: Revision có thể để trống, không NotEmpty — chỉ giới hạn độ dài khi có nhập.
        RuleFor(x => x.Revision)
            .MaximumLength(50).WithMessage("Revision tối đa 50 ký tự.");

        RuleFor(x => x.PlannedQuantity)
            .GreaterThan(0).WithMessage("Số lượng kế hoạch phải lớn hơn 0.");

        RuleFor(x => x.TaktTimeSeconds)
            .GreaterThan(0).WithMessage("Takt time phải lớn hơn 0.");

        RuleFor(x => x.StartTime)
            .NotEqual(default(DateTime)).WithMessage("Thời gian bắt đầu không được để trống.");

        RuleFor(x => x.OperatorNames)
            .NotEmpty().WithMessage("Tên nhân viên vận hành không được để trống.")
            .MaximumLength(500).WithMessage("Tên nhân viên vận hành tối đa 500 ký tự.");

        // US-05 AC7 (=US-21a AC1): "bắt buộc khi Lot hoàn toàn mới" cần tra DB (Service, không validate ở đây) —
        // chỉ ràng buộc format thuần túy: nếu CÓ nhập thì phải > 0.
        RuleFor(x => x.LotTotalQuantity)
            .GreaterThan(0).When(x => x.LotTotalQuantity.HasValue)
            .WithMessage("Tổng số lượng Lot phải lớn hơn 0.");
    }
}
