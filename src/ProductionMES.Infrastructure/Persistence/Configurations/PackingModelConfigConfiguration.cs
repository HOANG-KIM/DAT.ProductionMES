using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity PackingModelConfig (US-24/FR-24).</summary>
public class PackingModelConfigConfiguration : IEntityTypeConfiguration<PackingModelConfig>
{
    public void Configure(EntityTypeBuilder<PackingModelConfig> builder)
    {
        builder.ToTable("PackingModelConfig");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Model)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.ModelNormalized)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.PartName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.Manufacturer)
            .HasMaxLength(200);

        builder.Property(k => k.GrossWeight)
            .HasColumnType("decimal(10,2)");

        builder.Property(k => k.TemplateUpdatedByUserName)
            .HasMaxLength(100);

        builder.Property(k => k.UpdatedByUserName)
            .HasMaxLength(100);

        // KHÔNG unique index — tính duy nhất theo ModelNormalized do Service đảm bảo khi tạo mới (cùng nguyên
        // tắc Lot.Code, CLAUDE.md: entity không dùng khoá ngoại/ràng buộc unique kiểu truyền thống). Index thường
        // để tăng tốc tra cứu theo Model (AC9 — so khớp không phân biệt hoa/thường).
        builder.HasIndex(k => k.ModelNormalized);
    }
}
