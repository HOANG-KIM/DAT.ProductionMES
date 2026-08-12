namespace ProductionMES.Domain.Exceptions;

/// <summary>
/// Business exception dùng chung khi 1 thao tác vi phạm 1 quy tắc nghiệp vụ đã chốt (không phải lỗi validate
/// định dạng dữ liệu đơn thuần — những lỗi đó xử lý bằng FluentValidation ở tầng Api).
/// Vd: 1 Line chỉ có 1 kế hoạch active tại 1 thời điểm (US-05/AC2), trùng số thứ tự công đoạn trong kế hoạch
/// (US-03/AC4), cấu hình trình tự dẫn tới vòng lặp (US-03/AC5), trùng tên đăng nhập (US-22).
/// Được middleware xử lý exception toàn cục tại tầng Api ánh xạ sang HTTP 409 Conflict.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
