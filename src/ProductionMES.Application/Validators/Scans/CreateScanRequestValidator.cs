using FluentValidation;
using ProductionMES.Application.DTOs.Scans;

namespace ProductionMES.Application.Validators.Scans;

public class CreateScanRequestValidator : AbstractValidator<CreateScanRequest>
{
    public CreateScanRequestValidator()
    {
        RuleFor(x => x.TagCode)
            .NotEmpty().WithMessage("Mã tem không được để trống.")
            .MaximumLength(100).WithMessage("Mã tem tối đa 100 ký tự.");

        RuleFor(x => x.WorkStationId)
            .GreaterThan(0).WithMessage("WorkStationId không hợp lệ.");
    }
}
