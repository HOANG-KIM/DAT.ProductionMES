using FluentValidation;
using ProductionMES.Application.DTOs.WorkStations;

namespace ProductionMES.Application.Validators.WorkStations;

/// <summary>
/// Validator cho CreateWorkStationRequest (US-04). AC2: khi UseArduino = true, bắt buộc đủ thông tin
/// cổng COM. AC3: khi UseArduino = false, không bắt buộc nhập thông tin cổng COM.
/// </summary>
public class CreateWorkStationRequestValidator : AbstractValidator<CreateWorkStationRequest>
{
    public CreateWorkStationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên trạm không được để trống.")
            .MaximumLength(200).WithMessage("Tên trạm tối đa 200 ký tự.");

        RuleFor(x => x.LineId)
            .GreaterThan(0).WithMessage("Phải chọn 1 Line hợp lệ.");

        RuleFor(x => x.StageId)
            .GreaterThan(0).WithMessage("Phải chọn 1 công đoạn hợp lệ.");

        // AC2: trạm dùng Arduino bắt buộc nhập đủ thông tin cổng COM
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
