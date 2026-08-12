using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration cho entity Line (FR-01/US-01).
/// Tên bảng đặt "Line" (số ít, trùng tên entity) để đồng nhất với quy ước đặt tên bảng Việt hóa không dấu
/// đã dùng trong SRS cho các bảng khác (vd. WorkStation, LichSuScan, LichSuLenhArduino).
/// </summary>
public class LineConfiguration : IEntityTypeConfiguration<Line>
{
    public void Configure(EntityTypeBuilder<Line> builder)
    {
        builder.ToTable("Line");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Description)
            .HasMaxLength(1000);

        builder.Property(l => l.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
