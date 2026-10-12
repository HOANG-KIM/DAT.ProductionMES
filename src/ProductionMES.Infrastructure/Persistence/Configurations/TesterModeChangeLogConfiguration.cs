using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity TesterModeChangeLog (US-11 AC4).</summary>
public class TesterModeChangeLogConfiguration : IEntityTypeConfiguration<TesterModeChangeLog>
{
    public void Configure(EntityTypeBuilder<TesterModeChangeLog> builder)
    {
        builder.ToTable("TesterModeChangeLog");

        builder.HasKey(l => l.Id);

        // Lưu dạng chuỗi ("Tester"/"Bypass") — cùng quy ước Scan.Result/User.UserRole.
        builder.Property(l => l.FromMode).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.ToMode).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(l => l.Reason).IsRequired().HasMaxLength(200);

        // Khớp User.Username (snapshot, cùng Scan.ConfirmedByUserName).
        builder.Property(l => l.ApprovedByUserName).IsRequired().HasMaxLength(100);

        // Không dùng khoá ngoại ở DB — WorkStationId/ApprovedByUserId là cột tham chiếu thuần (CLAUDE.md).
        builder.HasIndex(l => new { l.WorkStationId, l.ChangedAtUtc });
    }
}
