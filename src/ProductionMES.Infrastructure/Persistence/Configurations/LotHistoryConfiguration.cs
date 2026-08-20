using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity LotHistory (truy vết thay đổi <see cref="Lot.TotalQuantity"/>).</summary>
public class LotHistoryConfiguration : IEntityTypeConfiguration<LotHistory>
{
    public void Configure(EntityTypeBuilder<LotHistory> builder)
    {
        builder.ToTable("LotHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.LotCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.ChangedByUserName)
            .HasMaxLength(100);

        // KHÔNG quy đổi UTC — lưu/đọc đúng giờ local nhà máy, cùng ngoại lệ với Lot.UpdatedAtUtc (API-Conventions.md mục 10).
        builder.Property(h => h.ChangedAtUtc)
            .IsRequired();

        // KHÔNG FK tới Lot (đúng convention dự án) — chỉ index để tăng tốc tra cứu lịch sử theo LotCode.
        builder.HasIndex(h => h.LotCode);
    }
}
