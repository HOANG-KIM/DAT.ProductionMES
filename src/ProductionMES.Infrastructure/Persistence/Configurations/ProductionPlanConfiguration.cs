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

        builder.Property(k => k.Customer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.Model)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.Lot)
            .IsRequired()
            .HasMaxLength(100);

        // AC2 (US-05): Revision có thể để trống — không IsRequired.
        builder.Property(k => k.Revision)
            .HasMaxLength(50);

        builder.Property(k => k.OperatorNames)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(k => k.TaktTimeSeconds)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.HasOne<Line>()
            .WithMany()
            .HasForeignKey(k => k.LineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hỗ trợ tra cứu nhanh "các kế hoạch của 1 Line" (màn hình Chọn kế hoạch — US-05b).
        builder.HasIndex(k => k.LineId);
    }
}
