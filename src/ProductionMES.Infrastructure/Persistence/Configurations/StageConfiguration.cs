using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity Stage (danh mục master) (FR-02/US-02).</summary>
public class StageConfiguration : IEntityTypeConfiguration<Stage>
{
    public void Configure(EntityTypeBuilder<Stage> builder)
    {
        builder.ToTable("Stage");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // US-25 (bổ sung 24/08/2026): mặc định false — mọi Stage hiện có (kể cả đã tạo trước US-25) giữ nguyên
        // luồng scan tiêu chuẩn (AC14) cho tới khi Admin chủ động đánh dấu đúng 1 Stage làm "Đóng thùng".
        builder.Property(c => c.IsPackingStage)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
