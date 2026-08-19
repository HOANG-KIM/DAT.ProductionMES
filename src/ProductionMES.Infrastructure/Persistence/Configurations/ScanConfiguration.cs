using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration cho entity Scan (FR-07/FR-08/FR-10, US-07/US-08).
/// KHÔNG có ràng buộc UNIQUE(TagCode, StageId) ở DB — 1 tem có thể có nhiều bản ghi bị từ chối, miễn tối đa 1
/// bản ghi có Result = Ok tại cùng (TagCode, StageId); ràng buộc "tối đa 1 Ok" xử lý ở ScanService (business
/// rule), không phải DB constraint (xem remarks tại entity Scan).
/// </summary>
public class ScanConfiguration : IEntityTypeConfiguration<Scan>
{
    public void Configure(EntityTypeBuilder<Scan> builder)
    {
        builder.ToTable("Scan");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TagCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.RejectionReason)
            .HasMaxLength(500);

        // US-18 (thay đổi 18/08/2026): NULLABLE — chỉ có giá trị khi Result = Ng (xem remarks tại entity Scan).
        // HasMaxLength(100) khớp User.Username (UserConfiguration) vì ConfirmedByUserName lưu snapshot Username.
        builder.Property(s => s.ConfirmedByUserName)
            .HasMaxLength(100);

        // US-10: 6 field snapshot từ ProductionPlan tại thời điểm scan (xem remarks tại entity Scan) — maxlength/kiểu
        // dữ liệu khớp đúng ProductionPlanConfiguration để giữ nguyên độ dài dữ liệu gốc.
        builder.Property(s => s.Customer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Model)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Lot)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Revision)
            .HasMaxLength(50);

        builder.Property(s => s.TaktTimeSeconds)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        // US-10 AC1/AC5 (bổ sung 19/08/2026): snapshot ProductionPlan.OperatorNames tại thời điểm scan — maxlength
        // khớp đúng ProductionPlanConfiguration, cùng nguyên tắc với 6 field snapshot ở trên.
        builder.Property(s => s.OperatorNames)
            .IsRequired()
            .HasMaxLength(500);

        // Lưu Result dạng chuỗi — cùng nguyên tắc đã áp dụng cho User.UserRole (dễ đọc/truy vấn trực tiếp trên
        // DB, ổn định khi thêm giá trị enum mới ở giữa, vd US-18 sẽ bổ sung "Ng").
        builder.Property(s => s.Result)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        // Không dùng khoá ngoại ở DB — StageId/LineId/WorkStationId/ProductionPlanId là cột tham chiếu thuần,
        // toàn vẹn xử lý ở ScanService.

        // Không unique — chỉ tăng tốc truy vấn chống trùng tem/kiểm tra công đoạn liền trước (FR-08, tra cứu
        // toàn hệ thống theo StageId, không lọc theo Line/kế hoạch).
        builder.HasIndex(s => new { s.TagCode, s.StageId });

        builder.HasIndex(s => s.ScannedAtUtc);

        // Tăng tốc truy vấn "đã Ok ở công đoạn này trong kế hoạch đang active chưa" (ScanService).
        builder.HasIndex(s => new { s.ProductionPlanId, s.StageId });
    }
}
