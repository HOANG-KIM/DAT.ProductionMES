using FluentValidation;
using ProductionMES.Application.DTOs.WorkStations;

namespace ProductionMES.Application.Validators.WorkStations;

/// <summary>
/// Validator cho CreateWorkStationRequest (US-04). US-11 AC12 (12/10/2026): KHÔNG còn bắt buộc ComPort/BaudRate/
/// CommandProtocol khi UseArduino = true — cấu hình kết nối máy thông điện nay ở appsettings.json cục bộ tại trạm
/// (section Tester); 3 trường cũ chỉ còn kiểm tra độ dài nếu có truyền (tương thích dữ liệu cũ).
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

        RuleFor(x => x.ComPort).MaximumLength(50);

        RuleFor(x => x.BaudRate)
            .GreaterThan(0).WithMessage("Baud rate phải lớn hơn 0.")
            .When(x => x.BaudRate.HasValue);

        RuleFor(x => x.CommandProtocol).MaximumLength(200);
    }
}
