using FluentValidation;
using ProductionMES.Application.DTOs.BreakWindows;

namespace ProductionMES.Application.Validators.BreakWindows;

/// <summary>
/// Validate hình dạng request (stateless) — chỉ kiểm tra EndTime > StartTime (AC5, phần đầu). Phần "không
/// chồng lấn với khung giờ nghỉ khác đã có của cùng Line" (AC5, phần sau) cần truy vấn dữ liệu hiện có của Line
/// nên đặt ở <see cref="ProductionMES.Application.Services.BreakWindows.BreakWindowService"/> (ném
/// <c>BusinessRuleException</c>), theo đúng convention business rule cần DB đã áp dụng cho
/// <c>ProductionPlanStageService</c> (không đặt logic truy vấn DB trong FluentValidation validator).
/// </summary>
public class CreateBreakWindowRequestValidator : AbstractValidator<CreateBreakWindowRequest>
{
    public CreateBreakWindowRequestValidator()
    {
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Giờ kết thúc phải lớn hơn giờ bắt đầu.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.");
    }
}
