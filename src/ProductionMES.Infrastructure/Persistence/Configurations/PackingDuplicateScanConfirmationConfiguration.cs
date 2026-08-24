using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity PackingDuplicateScanConfirmation (US-25 AC8).</summary>
public class PackingDuplicateScanConfirmationConfiguration : IEntityTypeConfiguration<PackingDuplicateScanConfirmation>
{
    public void Configure(EntityTypeBuilder<PackingDuplicateScanConfirmation> builder)
    {
        builder.ToTable("PackingDuplicateScanConfirmation");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TagCode)
            .IsRequired()
            .HasMaxLength(100);

        // HasMaxLength(100) khớp User.Username (cùng nguyên tắc Scan.ConfirmedByUserName/ReworkUnlock.UnlockedByUserName).
        builder.Property(c => c.ConfirmedByUserName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Note)
            .HasMaxLength(500);

        // Không dùng khoá ngoại ở DB (CLAUDE.md) — StageId/ScanId là cột tham chiếu thuần.

        // Tăng tốc tra cứu lịch sử xác nhận theo (TagCode, StageId), cùng idiom ReworkUnlockConfiguration.
        builder.HasIndex(c => new { c.TagCode, c.StageId });
    }
}
