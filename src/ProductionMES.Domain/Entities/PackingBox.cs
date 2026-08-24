using ProductionMES.Domain.Enums;

namespace ProductionMES.Domain.Entities;

/// <summary>
/// 1 thùng tại công đoạn "Đóng thùng" (US-25/FR-25) — đại diện đúng 1 lần đếm đủ số lượng sản phẩm theo Quy cách
/// đóng gói (<see cref="PackingModelConfig"/>, US-24) của 1 <see cref="ProductionPlan"/>, đánh số <see cref="BoxNo"/>
/// tăng dần.
/// </summary>
/// <remarks>
/// AC12 (mục 6 quy tắc 17 SRS): <see cref="TargetQuantity"/>/<see cref="ModelSnapshot"/>/<see cref="PartNameSnapshot"/>/
/// <see cref="ManufacturerSnapshot"/>/<see cref="GrossWeightSnapshot"/> đều là SNAPSHOT bất biến chụp lại từ
/// <see cref="PackingModelConfig"/> ĐÚNG tại thời điểm MỞ thùng này (tạo bản ghi) — KHÔNG tra cứu động qua
/// <see cref="PackingModelConfigId"/>. Nếu Admin sửa Quy cách đóng gói trong lúc thùng đang đóng dở, thùng đó vẫn
/// dùng đúng số liệu cũ; chỉ thùng MỞ SAU thời điểm sửa mới snapshot giá trị mới — cùng tinh thần snapshot đã áp
/// dụng cho <see cref="Scan"/> (US-10).
///
/// AC4: tại 1 thời điểm, mỗi (<see cref="ProductionPlanId"/>, <see cref="StageId"/>) chỉ có TỐI ĐA 1 bản ghi
/// <see cref="PackingBoxStatus.InProgress"/> (thùng hiện tại đang nhận tem) — khi đủ số lượng, bản ghi đó chuyển
/// <see cref="PackingBoxStatus.Completed"/> VÀ 1 bản ghi <see cref="PackingBoxStatus.InProgress"/> MỚI được tạo
/// ngay (BoxNo + 1), "sẵn sàng nhận tem mới" đúng nghĩa đen theo AC4 — không có khoảng trống nào giữa 2 thùng.
///
/// KHÔNG có ràng buộc khoá ngoại/unique kiểu truyền thống ở DB (CLAUDE.md) — <see cref="ProductionPlanId"/>/
/// <see cref="LineId"/>/<see cref="StageId"/>/<see cref="WorkStationId"/>/<see cref="PackingModelConfigId"/> là cột
/// tham chiếu thuần, toàn vẹn xử lý ở <c>PackingBoxService</c>.
/// </remarks>
public class PackingBox
{
    public int Id { get; set; }

    /// <summary>Kế hoạch sản xuất đang chạy tại "Đóng thùng" lúc mở thùng này.</summary>
    public int ProductionPlanId { get; set; }

    /// <summary>Line — ngữ cảnh, không dùng trong rule đếm/chống trùng (giống <see cref="Scan.LineId"/>).</summary>
    public int LineId { get; set; }

    /// <summary>Công đoạn master "Đóng thùng" (<see cref="Stage.IsPackingStage"/> = true) — cùng field dùng để xác định "thùng hiện tại" của 1 kế hoạch.</summary>
    public int StageId { get; set; }

    /// <summary>Trạm làm việc đã mở thùng này (AC5/AC6 — nơi Operator/Tổ trưởng nhập BoxNo bắt đầu hoặc thùng tự động mở tiếp theo).</summary>
    public int WorkStationId { get; set; }

    /// <summary>Số thùng (AC5/AC6/AC7) — do người dùng nhập lần đầu (AC5), tự tăng dần cho các thùng kế tiếp (AC4/AC6), có thể sửa lại bởi Supervisor (AC7).</summary>
    public int BoxNo { get; set; }

    public PackingBoxStatus Status { get; set; }

    /// <summary>Số lượng mục tiêu — SNAPSHOT <see cref="PackingModelConfig.PackingQuantity"/> tại thời điểm mở thùng (AC12, xem remarks).</summary>
    public int TargetQuantity { get; set; }

    /// <summary>Số lượng đã quét OK trong thùng này (AC2/AC9) — tăng dần từ 0, đạt <see cref="TargetQuantity"/> thì hoàn tất (AC4).</summary>
    public int ScannedQuantity { get; set; }

    /// <summary>Cấu hình đóng gói (US-24) dùng để snapshot khi mở thùng này — giữ lại để tham chiếu, KHÔNG dùng để tra cứu động dữ liệu in tem (xem các field Snapshot bên dưới).</summary>
    public int PackingModelConfigId { get; set; }

    /// <summary>Snapshot <see cref="PackingModelConfig.Model"/> tại thời điểm mở thùng (AC12/AC4 — in đúng lên tem thùng).</summary>
    public string ModelSnapshot { get; set; } = string.Empty;

    /// <summary>Snapshot <see cref="PackingModelConfig.PartName"/> tại thời điểm mở thùng.</summary>
    public string PartNameSnapshot { get; set; } = string.Empty;

    /// <summary>Snapshot <see cref="PackingModelConfig.Manufacturer"/> tại thời điểm mở thùng — có thể để trống.</summary>
    public string? ManufacturerSnapshot { get; set; }

    /// <summary>Snapshot <see cref="PackingModelConfig.GrossWeight"/> tại thời điểm mở thùng — có thể để trống.</summary>
    public decimal? GrossWeightSnapshot { get; set; }

    /// <summary>Thời điểm mở thùng — giờ local hệ thống (cùng quy ước <see cref="Scan.ScannedAtUtc"/>, KHÔNG quy đổi UTC, xem API-Conventions.md mục 10).</summary>
    public DateTime OpenedAtUtc { get; set; }

    /// <summary>Thời điểm hoàn tất (đủ số lượng, AC4) — <c>null</c> khi <see cref="Status"/> = <see cref="PackingBoxStatus.InProgress"/>.</summary>
    public DateTime? CompletedAtUtc { get; set; }
}
