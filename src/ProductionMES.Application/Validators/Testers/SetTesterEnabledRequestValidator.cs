using FluentValidation;
using ProductionMES.Application.DTOs.Testers;

namespace ProductionMES.Application.Validators.Testers;

/// <summary>Validator cho <see cref="SetTesterEnabledRequest"/> (US-11 AC4/AC5): phải có trạm hợp lệ và lý do.</summary>
public class SetTesterEnabledRequestValidator : AbstractValidator<SetTesterEnabledRequest>
{
    public SetTesterEnabledRequestValidator()
    {
        RuleFor(x => x.WorkStationId)
            .GreaterThan(0).WithMessage("Phải chọn 1 trạm hợp lệ.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Phải nhập lý do đổi chế độ máy thông điện.")
            .MaximumLength(200).WithMessage("Lý do tối đa 200 ký tự.");
    }
}
