using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration cho entity ProductionPlanStage (FR-03/US-03). Ràng buộc unique ở DB đóng vai trò
/// lưới an toàn bổ sung cho validate đã thực hiện ở Service (AC4 — không trùng số thứ tự; AC5 — không trùng
/// công đoạn trong cùng kế hoạch, đảm bảo không thể tạo vòng lặp).
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

        // AC4: không trùng số thứ tự trong cùng 1 kế hoạch.
        builder.HasIndex(k => new { k.ProductionPlanId, k.SequenceNumber }).IsUnique();

        // AC5: không trùng công đoạn trong cùng 1 kế hoạch — điều kiện cấu trúc đảm bảo không có vòng lặp.
        builder.HasIndex(k => new { k.ProductionPlanId, k.StageId }).IsUnique();
    }
}
