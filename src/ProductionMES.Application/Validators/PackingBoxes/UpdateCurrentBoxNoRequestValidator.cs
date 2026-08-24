using FluentValidation;
using ProductionMES.Application.DTOs.PackingBoxes;

namespace ProductionMES.Application.Validators.PackingBoxes;

/// <summary>US-25 AC7: validate request sửa số thùng hiện tại.</summary>
public class UpdateCurrentBoxNoRequestValidator : AbstractValidator<UpdateCurrentBoxNoRequest>
{
    public UpdateCurrentBoxNoRequestValidator()
    {
        RuleFor(x => x.WorkStationId)
            .GreaterThan(0).WithMessage("WorkStationId không hợp lệ.");

        RuleFor(x => x.NewBoxNo)
            .GreaterThan(0).WithMessage("Số thùng phải lớn hơn 0.");
    }
}
