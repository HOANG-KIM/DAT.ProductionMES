using FluentValidation;
using ProductionMES.Application.DTOs.PackingBoxes;

namespace ProductionMES.Application.Validators.PackingBoxes;

/// <summary>US-25 AC5: validate request nhập số thùng bắt đầu.</summary>
public class SetStartingBoxNoRequestValidator : AbstractValidator<SetStartingBoxNoRequest>
{
    public SetStartingBoxNoRequestValidator()
    {
        RuleFor(x => x.StartingBoxNo)
            .GreaterThan(0).WithMessage("Số thùng bắt đầu phải lớn hơn 0.");
    }
}
