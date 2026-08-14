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

        // Không dùng khoá ngoại ở DB — LineId/StageId là cột tham chiếu thuần; Line/Stage tra cứu độc lập qua
        // repository generic tương ứng khi cần.
    }
}
