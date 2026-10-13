using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity TesterEventLog (US-14 AC12).</summary>
public class TesterEventLogConfiguration : IEntityTypeConfiguration<TesterEventLog>
{
    public void Configure(EntityTypeBuilder<TesterEventLog> builder)
    {
        builder.ToTable("TesterEventLog");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).IsRequired().HasConversion<string>().HasMaxLength(30);

        builder.Property(e => e.TagCode).HasMaxLength(100);
        builder.Property(e => e.Detail).HasMaxLength(500);
        builder.Property(e => e.ApprovedByUserName).HasMaxLength(100);

        // Không dùng khoá ngoại ở DB (CLAUDE.md).
        builder.HasIndex(e => new { e.WorkStationId, e.OccurredAtUtc });
    }
}
