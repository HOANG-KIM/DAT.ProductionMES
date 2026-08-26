using FluentValidation;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Validators.Scans;

/// <summary>US-27 AC5/AC6: validate request xác nhận lưu 1 lượt scan bị hệ thống tự động từ chối.</summary>
public class ConfirmRejectedScanRequestValidator : AbstractValidator<ConfirmRejectedScanRequest>
{
    public ConfirmRejectedScanRequestValidator()
    {
        RuleFor(x => x.TagCode)
            .NotEmpty().WithMessage("Mã tem không được để trống.")
            .MaximumLength(100).WithMessage("Mã tem tối đa 100 ký tự.");

        RuleFor(x => x.StageId).GreaterThan(0).WithMessage("StageId không hợp lệ.");
        RuleFor(x => x.LineId).GreaterThan(0).WithMessage("LineId không hợp lệ.");
        RuleFor(x => x.WorkStationId).GreaterThan(0).WithMessage("WorkStationId không hợp lệ.");
        RuleFor(x => x.ProductionPlanId).GreaterThan(0).WithMessage("ProductionPlanId không hợp lệ.");

        // AC10: endpoint này CHỈ dành cho các ScanResult bị từ chối TỰ ĐỘNG — Ok lưu ngay ở ScansController.Create,
        // Ng đi qua ScanNgController (US-18) riêng, không qua đây.
        RuleFor(x => x.Result)
            .Must(result => result != ScanResult.Ok && result != ScanResult.Ng)
            .WithMessage("Chỉ xác nhận lưu được các lượt scan bị hệ thống tự động từ chối (khác Ok/Ng).");
    }
}
