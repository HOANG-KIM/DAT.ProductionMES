using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity PackingBox (US-25/FR-25).</summary>
public class PackingBoxConfiguration : IEntityTypeConfiguration<PackingBox>
{
    public void Configure(EntityTypeBuilder<PackingBox> builder)
    {
        builder.ToTable("PackingBox");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.ModelSnapshot)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.PartNameSnapshot)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.ManufacturerSnapshot)
            .HasMaxLength(200);

        builder.Property(b => b.GrossWeightSnapshot)
            .HasColumnType("decimal(10,2)");

        // Lưu Status dạng chuỗi — cùng nguyên tắc Scan.Result/User.UserRole (dễ đọc/truy vấn trực tiếp trên DB).
        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Không dùng khoá ngoại ở DB (CLAUDE.md) — ProductionPlanId/LineId/StageId/WorkStationId/PackingModelConfigId
        // là cột tham chiếu thuần, toàn vẹn xử lý ở PackingBoxService.

        // AC4/AC6: tìm nhanh thùng InProgress hiện tại của 1 (ProductionPlanId, StageId).
        builder.HasIndex(b => new { b.ProductionPlanId, b.StageId, b.Status });
    }
}
