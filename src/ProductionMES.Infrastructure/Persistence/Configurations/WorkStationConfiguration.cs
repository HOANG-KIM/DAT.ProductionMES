using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity WorkStation (FR-04/US-04).</summary>
public class WorkStationConfiguration : IEntityTypeConfiguration<WorkStation>
{
    public void Configure(EntityTypeBuilder<WorkStation> builder)
    {
        builder.ToTable("WorkStation");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.ComPort)
            .HasMaxLength(50);

        builder.Property(t => t.CommandProtocol)
            .HasMaxLength(200);

        builder.Property(t => t.UseArduino)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // FK thuần (không cấu hình navigation property 2 chiều ở giai đoạn này vì chưa cần truy vấn liên bảng
        // qua EF Include — Line/Stage tra cứu độc lập qua repository generic tương ứng khi cần).
        builder.HasOne<Line>()
            .WithMany()
            .HasForeignKey(t => t.LineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Stage>()
            .WithMany()
            .HasForeignKey(t => t.StageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
