using FluentValidation;
using ProductionMES.Application.DTOs.Testers;

namespace ProductionMES.Application.Validators.Testers;

/// <summary>Validator cho <see cref="RecordTesterEventRequest"/> (US-14 AC12).</summary>
public class RecordTesterEventRequestValidator : AbstractValidator<RecordTesterEventRequest>
{
    public RecordTesterEventRequestValidator()
    {
        RuleFor(x => x.EventType).IsInEnum().WithMessage("Loại sự kiện không hợp lệ.");
        RuleFor(x => x.TagCode).MaximumLength(100);
        RuleFor(x => x.Detail).MaximumLength(500);
        RuleFor(x => x.ApprovedByUserName).MaximumLength(100);
        RuleFor(x => x.TesterSeq).GreaterThanOrEqualTo(0).When(x => x.TesterSeq.HasValue);
    }
}
