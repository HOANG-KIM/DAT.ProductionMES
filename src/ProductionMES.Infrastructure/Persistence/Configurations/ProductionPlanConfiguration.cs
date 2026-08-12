using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity ProductionPlan (FR-05/US-05).</summary>
public class ProductionPlanConfiguration : IEntityTypeConfiguration<ProductionPlan>
{
    public void Configure(EntityTypeBuilder<ProductionPlan> builder)
    {
        builder.ToTable("ProductionPlan");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.ProductCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(k => k.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.Shift)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(k => k.TaktTimeSeconds)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(k => k.IsActive)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne<Line>()
            .WithMany()
            .HasForeignKey(k => k.LineId)
            .OnDelete(DeleteBehavior.Restrict);

        // AC2: hỗ trợ truy vấn nhanh "Line này đang có kế hoạch active nào không" khi kích hoạt kế hoạch mới.
        builder.HasIndex(k => new { k.LineId, k.IsActive });
    }
}
