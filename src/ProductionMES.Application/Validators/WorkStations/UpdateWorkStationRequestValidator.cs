using FluentValidation;
using ProductionMES.Application.DTOs.WorkStations;

namespace ProductionMES.Application.Validators.WorkStations;

/// <summary>Validator cho UpdateWorkStationRequest (US-04), cùng rule AC2/AC3 như khi tạo mới.</summary>
public class UpdateWorkStationRequestValidator : AbstractValidator<UpdateWorkStationRequest>
{
    public UpdateWorkStationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên trạm không được để trống.")
            .MaximumLength(200).WithMessage("Tên trạm tối đa 200 ký tự.");

        RuleFor(x => x.LineId)
            .GreaterThan(0).WithMessage("Phải chọn 1 Line hợp lệ.");

        RuleFor(x => x.StageId)
            .GreaterThan(0).WithMessage("Phải chọn 1 công đoạn hợp lệ.");

        RuleFor(x => x.ComPort)
            .NotEmpty().WithMessage("Cổng COM không được để trống khi trạm sử dụng Arduino.")
            .MaximumLength(50)
            .When(x => x.UseArduino);

        RuleFor(x => x.BaudRate)
            .NotNull().WithMessage("Baud rate không được để trống khi trạm sử dụng Arduino.")
            .GreaterThan(0).WithMessage("Baud rate phải lớn hơn 0.")
            .When(x => x.UseArduino);

        RuleFor(x => x.CommandProtocol)
            .NotEmpty().WithMessage("Giao thức lệnh không được để trống khi trạm sử dụng Arduino.")
            .MaximumLength(200)
            .When(x => x.UseArduino);
    }
}
