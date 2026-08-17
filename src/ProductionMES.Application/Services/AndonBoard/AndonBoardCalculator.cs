namespace ProductionMES.Application.Services.AndonBoard;

/// <summary>
/// Tính toán THUẦN TÚY (không phụ thuộc DB/IO) cho bảng PLAN/ACTUAL/BALANCE theo mốc giờ tại màn hình trạm
/// (US-09, FR-09/FR-09a). Tách riêng khỏi <see cref="AndonBoardService"/> để unit test đầy đủ các edge case trừ
/// giờ nghỉ (AC5/AC6) mà không cần mock repository.
/// </summary>
/// <remarks>
/// Quy ước thời gian: mọi tham số <see cref="DateTime"/> ở đây là giờ "tường" (wall-clock) tại nhà máy — CÙNG
/// quy ước với <see cref="ProductionMES.Domain.Entities.ProductionPlan.StartTime"/> (nhập trực tiếp tại trạm,
/// không quy đổi UTC ở bất kỳ đâu trong codebase hiện tại — xem <c>PlanSettingsViewModel</c> phía Station.Wpf)
/// và <see cref="ProductionMES.Domain.Entities.BreakWindow"/> (<see cref="TimeOnly"/> thuần túy, không gắn múi
/// giờ). Việc quy đổi <c>Scan.ScannedAtUtc</c> (lưu UTC) sang cùng hệ quy chiếu này thuộc trách nhiệm của
/// <see cref="AndonBoardService"/> (gọi <c>DateTime.ToLocalTime()</c>), KHÔNG phải class này — giả định máy chủ
/// API chạy cùng múi giờ với nhà máy (hệ thống hiện chưa xử lý đa múi giờ ở bất kỳ đâu khác, đây là giả định kế
/// thừa từ cách ProductionPlan.StartTime đã được xử lý từ US-05, không phải gap mới phát sinh ở US-09).
/// </remarks>
public static class AndonBoardCalculator
{
    private const decimal SecondsPerHour = 3600m;

    /// <summary>
    /// Số giây làm việc thực tế đã trôi qua từ <paramref name="startTime"/> đến <paramref name="at"/>, đã trừ
    /// phần giao với các khung giờ nghỉ (AC5/AC6/FR-09a). Nếu <paramref name="at"/> đang rơi vào giữa 1 khung
    /// giờ nghỉ, kết quả tự động "đứng lại" đúng bằng giá trị tại thời điểm bắt đầu khung nghỉ đó — hệ quả tự
    /// nhiên của việc chỉ trừ phần overlap tới đúng min(breakEnd, at), không cần xử lý case riêng.
    /// </summary>
    public static decimal ComputeElapsedWorkingSeconds(
        DateTime startTime, DateTime at, IReadOnlyList<(TimeOnly Start, TimeOnly End)> breakWindows)
    {
        if (at <= startTime)
        {
            return 0m;
        }

        var totalSeconds = (decimal)(at - startTime).TotalSeconds;
        var breakSeconds = breakWindows.Sum(bw => ComputeOverlapSeconds(startTime, at, bw));

        return Math.Max(0m, totalSeconds - breakSeconds);
    }

    /// <summary>
    /// PLAN lũy kế tại 1 thời điểm = sản lượng chuẩn/giờ (FR-06, dùng giá trị CHÍNH XÁC không làm tròn để tránh
    /// dồn sai số qua nhiều mốc giờ) × số giờ làm việc thực tế đã trôi qua (AC5), làm tròn về số nguyên gần nhất
    /// (đơn vị là sản phẩm — không có khái niệm "một phần sản phẩm").
    /// </summary>
    public static int ComputePlanCumulative(
        decimal standardQuantityPerHourExact, DateTime startTime, DateTime at, IReadOnlyList<(TimeOnly Start, TimeOnly End)> breakWindows)
    {
        var elapsedHours = ComputeElapsedWorkingSeconds(startTime, at, breakWindows) / SecondsPerHour;
        var plan = standardQuantityPerHourExact * elapsedHours;
        return (int)Math.Round(plan, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Danh sách mốc giờ CỐ ĐỊNH (đã qua) hiển thị trên bảng (AC6): <paramref name="startTime"/> + 1h, +2h, ...
    /// cho tới TRƯỚC <paramref name="now"/> — KHÔNG bao gồm "now" (dòng "hiện tại" luôn do
    /// <see cref="AndonBoardService"/> thêm riêng, cập nhật liên tục). Giới hạn <paramref name="maxRows"/> để
    /// phòng vệ dữ liệu StartTime bất thường (vd kế hoạch tạo từ rất lâu, cấu hình sai) không làm bảng phình vô
    /// hạn.
    /// </summary>
    public static IReadOnlyList<DateTime> BuildPastHourMarks(DateTime startTime, DateTime now, int maxRows)
    {
        var marks = new List<DateTime>();
        if (now <= startTime)
        {
            return marks;
        }

        var mark = startTime.AddHours(1);
        while (mark < now && marks.Count < maxRows)
        {
            marks.Add(mark);
            mark = mark.AddHours(1);
        }

        return marks;
    }

    /// <summary>
    /// Số giây giao nhau giữa [<paramref name="rangeStart"/>, <paramref name="rangeEnd"/>] và khung giờ nghỉ
    /// <paramref name="breakWindow"/> — lặp qua từng ngày trong khoảng vì <see cref="TimeOnly"/> chỉ đại diện 1
    /// giờ trong ngày, lặp lại hàng ngày (BreakWindow.EndTime luôn &gt; StartTime, không có khung "qua đêm" —
    /// đã đảm bảo bởi <c>BreakWindowService</c> AC5).
    /// </summary>
    private static decimal ComputeOverlapSeconds(DateTime rangeStart, DateTime rangeEnd, (TimeOnly Start, TimeOnly End) breakWindow)
    {
        var total = 0m;
        for (var day = rangeStart.Date; day <= rangeEnd.Date; day = day.AddDays(1))
        {
            var breakStart = day.Add(breakWindow.Start.ToTimeSpan());
            var breakEnd = day.Add(breakWindow.End.ToTimeSpan());

            var overlapStart = breakStart > rangeStart ? breakStart : rangeStart;
            var overlapEnd = breakEnd < rangeEnd ? breakEnd : rangeEnd;

            if (overlapEnd > overlapStart)
            {
                total += (decimal)(overlapEnd - overlapStart).TotalSeconds;
            }
        }

        return total;
    }
}
