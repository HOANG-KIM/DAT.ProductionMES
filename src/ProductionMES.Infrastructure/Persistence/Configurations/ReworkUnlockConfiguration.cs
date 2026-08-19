using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity ReworkUnlock (US-19, FR-19).</summary>
public class ReworkUnlockConfiguration : IEntityTypeConfiguration<ReworkUnlock>
{
    public void Configure(EntityTypeBuilder<ReworkUnlock> builder)
    {
        builder.ToTable("ReworkUnlock");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.TagCode)
            .IsRequired()
            .HasMaxLength(100);

        // Khớp User.Username (UserConfiguration) — UnlockedByUserName lưu snapshot Username, cùng quy ước
        // Scan.ConfirmedByUserName (US-18).
        builder.Property(u => u.UnlockedByUserName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Note)
            .HasMaxLength(500);

        // KHÔNG quy đổi UTC — lưu/đọc đúng giờ local nhà máy (đổi ý 19/08/2026 — xem API-Conventions.md mục 10).
        builder.Property(u => u.UnlockedAtUtc)
            .IsRequired();

        // Không dùng khoá ngoại ở DB — StageId là cột tham chiếu thuần (CLAUDE.md).

        // Tăng tốc truy vấn "đang khóa rework hay không" (ReworkLockCalculator) — tra cứu toàn hệ thống theo
        // (TagCode, StageId), cùng cách ScanConfiguration đã làm cho bảng Scan.
        builder.HasIndex(u => new { u.TagCode, u.StageId });

        builder.HasIndex(u => u.UnlockedAtUtc);
    }
}
