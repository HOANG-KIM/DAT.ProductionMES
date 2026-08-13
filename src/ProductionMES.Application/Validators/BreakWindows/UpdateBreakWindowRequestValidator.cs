using FluentValidation;
using ProductionMES.Application.DTOs.BreakWindows;

namespace ProductionMES.Application.Validators.BreakWindows;

/// <summary>Xem ghi chú thiết kế tại <see cref="CreateBreakWindowRequestValidator"/> — chồng lấn kiểm tra ở Service.</summary>
public class UpdateBreakWindowRequestValidator : AbstractValidator<UpdateBreakWindowRequest>
{
    public UpdateBreakWindowRequestValidator()
    {
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Giờ kết thúc phải lớn hơn giờ bắt đầu.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.");
    }
}
