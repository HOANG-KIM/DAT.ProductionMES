using System.Globalization;

namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Quy đổi 2 chiều giữa số giây (<c>TaktTimeSeconds</c> — đơn vị lưu trữ/gửi API, xem SRS FR-05 "đơn vị lưu trữ/
/// tính toán khác đơn vị nhập liệu") và định dạng phút:giây hiển thị/nhập trên UI trạm (US-05 AC1e). Dùng chung
/// cho cả chiều hiển thị (DTO nhận từ API) lẫn chiều nhập liệu (ViewModel gửi API) để tránh lệch quy tắc.
/// </summary>
public static class TaktTimeFormat
{
    /// <summary>Quy đổi số giây sang chuỗi hiển thị "m:ss" (phút không zero-pad, giây luôn 2 chữ số).</summary>
    public static string ToDisplay(decimal seconds)
    {
        var totalSeconds = (long)Math.Round(seconds, MidpointRounding.AwayFromZero);
        if (totalSeconds < 0)
        {
            totalSeconds = 0;
        }

        var minutes = totalSeconds / 60;
        var secondsPart = totalSeconds % 60;
        return $"{minutes}:{secondsPart:D2}";
    }

    /// <summary>
    /// Quy đổi số giây sang chuỗi hiển thị dài "mm phút ss giây" (vd "01 phút 00 giây") — dùng cho màn hình CHỈ
    /// ĐỌC (Andon board, US-09) để tránh nhầm giữa phút/giây như định dạng "m:ss" ngắn gọn của <see cref="ToDisplay"/>.
    /// KHÔNG dùng cho luồng nhập liệu (giữ nguyên <see cref="ToDisplay"/>/<see cref="TryParse"/> cho <c>PlanSettingsPage</c>,
    /// đổi định dạng ở đó sẽ phá luồng round-trip hiển thị → nhập → parse lại đã có từ US-05 AC1e).
    /// </summary>
    public static string ToVerboseDisplay(decimal seconds)
    {
        var totalSeconds = (long)Math.Round(seconds, MidpointRounding.AwayFromZero);
        if (totalSeconds < 0)
        {
            totalSeconds = 0;
        }

        var minutes = totalSeconds / 60;
        var secondsPart = totalSeconds % 60;
        return $"{minutes:D2} phút {secondsPart:D2} giây";
    }

    /// <summary>
    /// Parse chuỗi "m:ss" người dùng nhập thành số giây nguyên. Chỉ chấp nhận số nguyên giây (không phần thập
    /// phân), giây bắt buộc đúng 2 chữ số trong khoảng 00-59. Trả về <c>false</c> kèm <paramref name="error"/> mô
    /// tả lỗi nếu không hợp lệ — KHÔNG tự sửa ngầm giá trị (AC1e).
    /// </summary>
    public static bool TryParse(string? text, out decimal seconds, out string? error)
    {
        seconds = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Takt time không được để trống.";
            return false;
        }

        var parts = text.Trim().Split(':');
        if (parts.Length != 2)
        {
            error = "Takt time phải nhập theo định dạng phút:giây, ví dụ \"1:30\".";
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) || minutes < 0)
        {
            error = "Phần phút của Takt time không hợp lệ, phải là số nguyên không âm.";
            return false;
        }

        if (parts[1].Length != 2 || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var secondsPart)
            || secondsPart < 0 || secondsPart > 59)
        {
            error = "Phần giây của Takt time phải là số nguyên 2 chữ số từ 00 đến 59.";
            return false;
        }

        seconds = minutes * 60 + secondsPart;
        return true;
    }
}
