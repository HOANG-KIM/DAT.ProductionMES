using ProductionMES.Application.Services.AndonBoard;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="AndonBoardCalculator"/> (US-09, FR-09/FR-09a) — tập trung vào AC5/AC6 (trừ khung giờ
/// nghỉ khi tính PLAN lũy kế), vì đây là logic dễ sai edge case nhất theo ghi chú của task.
/// </summary>
public class AndonBoardCalculatorTests
{
    private static readonly DateTime ShiftStart = new(2026, 8, 17, 7, 35, 0);

    // AC1/AC5 (trường hợp không có khung giờ nghỉ) — elapsed = chênh lệch thô.
    [Fact]
    public void ComputeElapsedWorkingSeconds_KhongCoKhungGioNghi_BangChenhLechTho()
    {
        var at = ShiftStart.AddHours(2);

        var elapsed = AndonBoardCalculator.ComputeElapsedWorkingSeconds(ShiftStart, at, breakWindows: Array.Empty<(TimeOnly, TimeOnly)>());

        Assert.Equal(7200m, elapsed);
    }

    // Phòng vệ: "at" trước "startTime" (vd đồng hồ lệch, hoặc gọi trước giờ bắt đầu ca) -> 0, không âm.
    [Fact]
    public void ComputeElapsedWorkingSeconds_AtTruocStartTime_TraVe0()
    {
        var at = ShiftStart.AddMinutes(-10);

        var elapsed = AndonBoardCalculator.ComputeElapsedWorkingSeconds(ShiftStart, at, Array.Empty<(TimeOnly, TimeOnly)>());

        Assert.Equal(0m, elapsed);
    }

    // AC5 — "at" đang RƠI VÀO GIỮA khung giờ nghỉ: elapsed phải "đứng lại" đúng bằng giá trị tại thời điểm bắt đầu nghỉ.
    [Fact]
    public void ComputeElapsedWorkingSeconds_AtRoiVaoGiuaKhungGioNghi_DungLaiDungGiaTriLucBatDauNghi()
    {
        // Ca bắt đầu 07:35, nghỉ trưa 12:00-13:00, đang xét tại 12:30 (giữa khung nghỉ).
        var breakWindows = new (TimeOnly, TimeOnly)[] { (new TimeOnly(12, 0), new TimeOnly(13, 0)) };
        var at = new DateTime(2026, 8, 17, 12, 30, 0);

        var elapsed = AndonBoardCalculator.ComputeElapsedWorkingSeconds(ShiftStart, at, breakWindows);

        // Elapsed thực tế phải bằng đúng khoảng 07:35 -> 12:00 (thời điểm bắt đầu nghỉ), KHÔNG cộng thêm 30 phút đã trôi trong lúc nghỉ.
        var expected = (decimal)(new DateTime(2026, 8, 17, 12, 0, 0) - ShiftStart).TotalSeconds;
        Assert.Equal(expected, elapsed);
    }

    // AC5 — sau khi hết nghỉ, tính tiếp bình thường theo thời gian làm việc thực tế đã trôi qua (trừ đúng 1h nghỉ).
    [Fact]
    public void ComputeElapsedWorkingSeconds_SauKhiHetNghi_TinhTiepTheoThoiGianLamViecThucTe()
    {
        var breakWindows = new (TimeOnly, TimeOnly)[] { (new TimeOnly(12, 0), new TimeOnly(13, 0)) };
        var at = new DateTime(2026, 8, 17, 13, 15, 0);

        var elapsed = AndonBoardCalculator.ComputeElapsedWorkingSeconds(ShiftStart, at, breakWindows);

        // Tổng thời gian thô (07:35 -> 13:15) trừ đúng 1 giờ nghỉ trọn vẹn (12:00-13:00).
        var rawSeconds = (decimal)(at - ShiftStart).TotalSeconds;
        Assert.Equal(rawSeconds - 3600m, elapsed);
    }

    // AC5 (trước khung giờ nghỉ) — "at" chưa tới khung giờ nghỉ -> không bị trừ gì cả.
    [Fact]
    public void ComputeElapsedWorkingSeconds_AtTruocKhungGioNghi_KhongTruGiChiCa()
    {
        var breakWindows = new (TimeOnly, TimeOnly)[] { (new TimeOnly(12, 0), new TimeOnly(13, 0)) };
        var at = new DateTime(2026, 8, 17, 9, 0, 0);

        var elapsed = AndonBoardCalculator.ComputeElapsedWorkingSeconds(ShiftStart, at, breakWindows);

        var rawSeconds = (decimal)(at - ShiftStart).TotalSeconds;
        Assert.Equal(rawSeconds, elapsed);
    }

    // AC5 — nhiều khung giờ nghỉ, "at" sau cả 2 khung -> trừ đủ cả 2.
    [Fact]
    public void ComputeElapsedWorkingSeconds_NhieuKhungGioNghi_TruDuTatCa()
    {
        var breakWindows = new (TimeOnly, TimeOnly)[]
        {
            (new TimeOnly(9, 0), new TimeOnly(9, 15)), // nghỉ giữa giờ 15 phút
            (new TimeOnly(12, 0), new TimeOnly(13, 0)), // nghỉ trưa 1 giờ
        };
        var at = new DateTime(2026, 8, 17, 14, 0, 0);

        var elapsed = AndonBoardCalculator.ComputeElapsedWorkingSeconds(ShiftStart, at, breakWindows);

        var rawSeconds = (decimal)(at - ShiftStart).TotalSeconds;
        Assert.Equal(rawSeconds - (15 * 60m) - 3600m, elapsed);
    }

    // AC6 — mốc giờ hiển thị rơi vào khung giờ nghỉ: PLAN tại mốc đó vẫn được tính (không lỗi), và đúng bằng giá trị lúc bắt đầu nghỉ.
    [Fact]
    public void ComputePlanCumulative_MocGioRoiVaoKhungGioNghi_GiuNguyenGiaTriLucBatDauNghi()
    {
        var breakWindows = new (TimeOnly, TimeOnly)[] { (new TimeOnly(12, 0), new TimeOnly(13, 0)) };
        // Sản lượng chuẩn/giờ = 60 (takt 60s) — dễ tính tay.
        const decimal standardQuantityPerHour = 60m;

        // Mốc giờ 12:35 (rơi vào giữa khung nghỉ 12:00-13:00), ca bắt đầu 07:35 -> 4h25m thô, trừ về đúng 4h20m (07:35->12:00 - dùng lại số liệu ở trên: 4h25m thực ra là 07:35->12:00 = 4h25m).
        var mark = new DateTime(2026, 8, 17, 12, 35, 0);

        var plan = AndonBoardCalculator.ComputePlanCumulative(standardQuantityPerHour, ShiftStart, mark, breakWindows);

        // Elapsed đứng lại tại 07:35 -> 12:00 = 4h25m = 4.41666h -> PLAN = 60 * 4.41666 = 265 (làm tròn).
        var expectedElapsedHours = (decimal)(new DateTime(2026, 8, 17, 12, 0, 0) - ShiftStart).TotalHours;
        var expectedPlan = (int)Math.Round(standardQuantityPerHour * expectedElapsedHours, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedPlan, plan);
    }

    // AC1 — không có khung giờ nghỉ, PLAN = sản lượng chuẩn/giờ x số giờ đã trôi qua.
    [Fact]
    public void ComputePlanCumulative_KhongCoKhungGioNghi_TinhDungCongThucChuan()
    {
        const decimal standardQuantityPerHour = 34.62m;
        var at = ShiftStart.AddHours(3);

        var plan = AndonBoardCalculator.ComputePlanCumulative(standardQuantityPerHour, ShiftStart, at, Array.Empty<(TimeOnly, TimeOnly)>());

        Assert.Equal((int)Math.Round(standardQuantityPerHour * 3, MidpointRounding.AwayFromZero), plan);
    }

    // AC6 — danh sách mốc giờ cố định: StartTime + 1h, +2h,... cho tới TRƯỚC "now", không bao gồm "now".
    [Fact]
    public void BuildPastHourMarks_TraVeDungCacMocGioTronDaQua_KhongBaoGomNow()
    {
        var now = ShiftStart.AddHours(3).AddMinutes(40); // vd 11:15 nếu ShiftStart = 07:35

        var marks = AndonBoardCalculator.BuildPastHourMarks(ShiftStart, now, maxRows: 24);

        Assert.Equal(new[]
        {
            ShiftStart.AddHours(1),
            ShiftStart.AddHours(2),
            ShiftStart.AddHours(3),
        }, marks);
    }

    // Now <= StartTime (chưa tới giờ bắt đầu ca) -> danh sách rỗng.
    [Fact]
    public void BuildPastHourMarks_NowTruocHoacBangStartTime_TraVeRong()
    {
        var marks = AndonBoardCalculator.BuildPastHourMarks(ShiftStart, ShiftStart, maxRows: 24);

        Assert.Empty(marks);
    }

    // Phòng vệ: StartTime bất thường (rất lâu trước "now") -> không phình vô hạn, tôn trọng maxRows.
    [Fact]
    public void BuildPastHourMarks_StartTimeRatXa_GioiHanDungMaxRows()
    {
        var longAgoStart = ShiftStart.AddDays(-10);

        var marks = AndonBoardCalculator.BuildPastHourMarks(longAgoStart, ShiftStart, maxRows: 24);

        Assert.Equal(24, marks.Count);
    }
}
