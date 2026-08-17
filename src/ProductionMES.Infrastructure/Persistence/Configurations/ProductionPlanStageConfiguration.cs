using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration cho entity ProductionPlanStage (FR-05a/US-05a) — entity này giờ CHỈ đại diện vòng
/// đời <see cref="ProductionPlanStage.PlanStatus"/> của 1 cặp (Kế hoạch, Công đoạn), không còn mang trình tự
/// (đã chuyển sang <see cref="ProductionMES.Domain.Entities.LineStageSequence"/>, xem remarks tại entity).
/// </summary>
public class ProductionPlanStageConfiguration : IEntityTypeConfiguration<ProductionPlanStage>
{
    public void Configure(EntityTypeBuilder<ProductionPlanStage> builder)
    {
        builder.ToTable("ProductionPlanStage");

        builder.HasKey(k => k.Id);

        // Không dùng khoá ngoại ở DB (ProductionPlanId/StageId/LineId là cột tham chiếu thuần) — toàn vẹn quan
        // hệ ("tối đa 1 kế hoạch Running" ở PlanStatus) xử lý ở Service.

        // Lưu PlanStatus dạng chuỗi — cùng nguyên tắc đã áp dụng cho Scan.Result/User.UserRole (dễ đọc/truy vấn
        // trực tiếp trên DB, ổn định khi thêm giá trị enum mới ở giữa).
        builder.Property(k => k.PlanStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Không trùng công đoạn trong cùng 1 kế hoạch — mỗi cặp (Kế hoạch, Công đoạn) chỉ có đúng 1 bản ghi vòng đời.
        builder.HasIndex(k => new { k.ProductionPlanId, k.StageId }).IsUnique();

        // US-05a AC1/AC2: hỗ trợ tra cứu nhanh "cặp (Line, Công đoạn) này đang có kế hoạch nào Running" khi Áp
        // dụng kế hoạch mới (không unique ở DB — ràng buộc "tối đa 1 Running" xử lý ở Service, cùng nguyên tắc
        // đã áp dụng cho "tối đa 1 Scan Ok" ở Scan/ScanService, vì MySQL 5.7.16 không hỗ trợ filtered/partial
        // unique index để chỉ áp dụng riêng cho giá trị Running).
        builder.HasIndex(k => new { k.LineId, k.StageId, k.PlanStatus });
    }
}
