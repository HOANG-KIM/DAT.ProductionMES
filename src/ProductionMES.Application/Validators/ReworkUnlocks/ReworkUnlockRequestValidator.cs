using FluentValidation;
using ProductionMES.Application.DTOs.ReworkUnlocks;

namespace ProductionMES.Application.Validators.ReworkUnlocks;

/// <summary>US-19 AC2: validate request "Mở khóa rework".</summary>
public class ReworkUnlockRequestValidator : AbstractValidator<ReworkUnlockRequest>
{
    public ReworkUnlockRequestValidator()
    {
        RuleFor(x => x.TagCode)
            .NotEmpty().WithMessage("Mã tem không được để trống.")
            .MaximumLength(100).WithMessage("Mã tem tối đa 100 ký tự.");

        RuleFor(x => x.WorkStationId)
            .GreaterThan(0).WithMessage("WorkStationId không hợp lệ.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.");
    }
}
