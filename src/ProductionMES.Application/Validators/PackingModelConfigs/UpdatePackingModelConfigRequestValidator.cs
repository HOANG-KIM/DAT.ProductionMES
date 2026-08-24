using FluentValidation;
using ProductionMES.Application.DTOs.PackingModelConfigs;

namespace ProductionMES.Application.Validators.PackingModelConfigs;

/// <summary>Validate AC7 cho AC2 (sửa cấu hình đã có) — Model không nằm trong request này (không đổi được, xem <see cref="UpdatePackingModelConfigRequest"/>).</summary>
public class UpdatePackingModelConfigRequestValidator : AbstractValidator<UpdatePackingModelConfigRequest>
{
    public UpdatePackingModelConfigRequestValidator()
    {
        RuleFor(x => x.PartName)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(200).WithMessage("Tên sản phẩm tối đa 200 ký tự.");

        RuleFor(x => x.PackingQuantity)
            .GreaterThan(0).WithMessage("Quy cách đóng gói phải lớn hơn 0.");

        RuleFor(x => x.GrossWeight)
            .GreaterThan(0).WithMessage("Khối lượng phải lớn hơn 0.")
            .When(x => x.GrossWeight.HasValue);

        RuleFor(x => x.Manufacturer)
            .MaximumLength(200).WithMessage("Nhà sản xuất tối đa 200 ký tự.");
    }
}
