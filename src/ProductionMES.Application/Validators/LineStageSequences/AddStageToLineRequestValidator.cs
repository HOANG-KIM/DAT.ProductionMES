using FluentValidation;
using ProductionMES.Application.DTOs.LineStageSequences;

namespace ProductionMES.Application.Validators.LineStageSequences;

public class AddStageToLineRequestValidator : AbstractValidator<AddStageToLineRequest>
{
    public AddStageToLineRequestValidator()
    {
        RuleFor(x => x.StageId)
            .GreaterThan(0).WithMessage("Phải chọn 1 công đoạn hợp lệ.");

        RuleFor(x => x.SequenceNumber)
            .GreaterThan(0).WithMessage("Số thứ tự phải lớn hơn 0.")
            .When(x => x.SequenceNumber.HasValue);
    }
}
