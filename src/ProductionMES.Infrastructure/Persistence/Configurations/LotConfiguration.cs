using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity Lot (US-21a/FR-21a, viết lại hoàn toàn 19/08/2026).</summary>
public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.ToTable("Lot");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.UpdatedByUserName)
            .HasMaxLength(100);

        builder.Property(l => l.UpdatedAtUtc)
            .IsRequired();

        // KHÔNG unique index — tính duy nhất của Code do Service đảm bảo khi upsert (CLAUDE.md, entity không dùng
        // khoá ngoại/ràng buộc unique kiểu truyền thống). Index thường để tăng tốc tra cứu theo Code.
        builder.HasIndex(l => l.Code);
    }
}
