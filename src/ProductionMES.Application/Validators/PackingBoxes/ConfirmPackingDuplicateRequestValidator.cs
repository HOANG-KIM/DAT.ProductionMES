using FluentValidation;
using ProductionMES.Application.DTOs.PackingBoxes;

namespace ProductionMES.Application.Validators.PackingBoxes;

/// <summary>US-25 AC8: validate request xác nhận đã biết tình huống tem trùng.</summary>
public class ConfirmPackingDuplicateRequestValidator : AbstractValidator<ConfirmPackingDuplicateRequest>
{
    public ConfirmPackingDuplicateRequestValidator()
    {
        RuleFor(x => x.WorkStationId)
            .GreaterThan(0).WithMessage("WorkStationId không hợp lệ.");

        RuleFor(x => x.TagCode)
            .NotEmpty().WithMessage("Mã tem không được để trống.")
            .MaximumLength(100).WithMessage("Mã tem tối đa 100 ký tự.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.");
    }
}
