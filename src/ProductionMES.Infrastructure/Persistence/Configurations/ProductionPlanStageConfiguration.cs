using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration cho entity ProductionPlanStage (FR-03/US-03, FR-05a/US-05a). Ràng buộc unique ở DB
/// đóng vai trò lưới an toàn bổ sung cho validate đã thực hiện ở Service (AC4 — không trùng số thứ tự; AC5 —
/// không trùng công đoạn trong cùng kế hoạch, đảm bảo không thể tạo vòng lặp).
/// </summary>
public class ProductionPlanStageConfiguration : IEntityTypeConfiguration<ProductionPlanStage>
{
    public void Configure(EntityTypeBuilder<ProductionPlanStage> builder)
    {
        builder.ToTable("ProductionPlanStage");

        builder.HasKey(k => k.Id);

        builder.HasOne<ProductionPlan>()
            .WithMany()
            .HasForeignKey(k => k.ProductionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Stage>()
            .WithMany()
            .HasForeignKey(k => k.StageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Line>()
            .WithMany()
            .HasForeignKey(k => k.LineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lưu PlanStatus dạng chuỗi — cùng nguyên tắc đã áp dụng cho Scan.Result/User.UserRole (dễ đọc/truy vấn
        // trực tiếp trên DB, ổn định khi thêm giá trị enum mới ở giữa).
        builder.Property(k => k.PlanStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // AC4: không trùng số thứ tự trong cùng 1 kế hoạch.
        builder.HasIndex(k => new { k.ProductionPlanId, k.SequenceNumber }).IsUnique();

        // AC5: không trùng công đoạn trong cùng 1 kế hoạch — điều kiện cấu trúc đảm bảo không có vòng lặp.
        builder.HasIndex(k => new { k.ProductionPlanId, k.StageId }).IsUnique();

        // US-05a AC1/AC2: hỗ trợ tra cứu nhanh "cặp (Line, Công đoạn) này đang có kế hoạch nào Running" khi Áp
        // dụng kế hoạch mới (không unique ở DB — ràng buộc "tối đa 1 Running" xử lý ở Service, cùng nguyên tắc
        // đã áp dụng cho "tối đa 1 Scan Ok" ở Scan/ScanService, vì MySQL 5.7.16 không hỗ trợ filtered/partial
        // unique index để chỉ áp dụng riêng cho giá trị Running).
        builder.HasIndex(k => new { k.LineId, k.StageId, k.PlanStatus });
    }
}
