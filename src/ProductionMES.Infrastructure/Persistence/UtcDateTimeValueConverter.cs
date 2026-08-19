using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ProductionMES.Infrastructure.Persistence;

/// <summary>
/// EF Core (Pomelo/MySQL) trả về DateTime với Kind = Unspecified khi đọc lại cột `datetime` từ DB (MySQL
/// không lưu thông tin timezone) — dù giá trị SỐ vẫn đúng UTC (đã ghi bằng DateTime.UtcNow), việc mất Kind
/// khiến System.Text.Json KHÔNG thêm hậu tố "Z" khi serialize (vi phạm API-Conventions.md mục 10), làm
/// web-admin/Station.Wpf hiểu nhầm là giờ địa phương khi hiển thị (dayjs không có plugin UTC). Converter này
/// ép Kind = Utc lại đúng lúc ĐỌC (giá trị số giữ nguyên, chỉ gắn lại nhãn) — áp dụng cho MỌI property có hậu
/// tố "Utc" trong tên (xem danh sách trong từng *Configuration.cs).
/// </summary>
public class UtcDateTimeValueConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeValueConverter()
        : base(
            toDb => toDb,
            fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc))
    {
    }
}

/// <summary>Phiên bản nullable của <see cref="UtcDateTimeValueConverter"/> — dùng cho các property Utc kiểu <c>DateTime?</c>.</summary>
public class NullableUtcDateTimeValueConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeValueConverter()
        : base(
            toDb => toDb,
            fromDb => fromDb.HasValue ? DateTime.SpecifyKind(fromDb.Value, DateTimeKind.Utc) : fromDb)
    {
    }
}
