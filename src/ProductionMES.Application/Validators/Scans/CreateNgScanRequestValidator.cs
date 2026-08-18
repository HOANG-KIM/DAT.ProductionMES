using FluentValidation;
using ProductionMES.Application.DTOs.Scans;

namespace ProductionMES.Application.Validators.Scans;

/// <summary>US-18 AC3: bắt buộc nhập lý do lỗi trước khi ghi nhận Scan NG.</summary>
public class CreateNgScanRequestValidator : AbstractValidator<CreateNgScanRequest>
{
    public CreateNgScanRequestValidator()
    {
        RuleFor(x => x.TagCode)
            .NotEmpty().WithMessage("Mã tem không được để trống.")
            .MaximumLength(100).WithMessage("Mã tem tối đa 100 ký tự.");

        RuleFor(x => x.WorkStationId)
            .GreaterThan(0).WithMessage("WorkStationId không hợp lệ.");

        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("Phải nhập lý do lỗi trước khi ghi nhận Scan NG.")
            .MaximumLength(500).WithMessage("Lý do lỗi tối đa 500 ký tự.");
    }
}
