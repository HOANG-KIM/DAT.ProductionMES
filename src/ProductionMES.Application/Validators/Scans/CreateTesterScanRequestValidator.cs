using FluentValidation;
using ProductionMES.Application.DTOs.Scans;

namespace ProductionMES.Application.Validators.Scans;

public class CreateTesterScanRequestValidator : AbstractValidator<CreateTesterScanRequest>
{
    public CreateTesterScanRequestValidator()
    {
        RuleFor(x => x.TagCode)
            .NotEmpty().WithMessage("Mã tem không được để trống.")
            .MaximumLength(100).WithMessage("Mã tem tối đa 100 ký tự.");

        RuleFor(x => x.TesterBootId)
            .NotEmpty().WithMessage("Thiếu BOOT id của máy thông điện.")
            .MaximumLength(8).WithMessage("BOOT id tối đa 8 ký tự.");

        RuleFor(x => x.TesterSeq)
            .GreaterThan(0).WithMessage("SEQ kết quả của máy phải lớn hơn 0.");

        RuleFor(x => x.GoldenCrc)
            .NotEmpty().WithMessage("Thiếu GM (golden matrix CRC) của máy thông điện.")
            .MaximumLength(4).WithMessage("GM tối đa 4 ký tự.");

        RuleFor(x => x.FirmwareVersion)
            .NotEmpty().WithMessage("Thiếu phiên bản firmware của máy thông điện.")
            .MaximumLength(16).WithMessage("Phiên bản firmware tối đa 16 ký tự.");
    }
}
