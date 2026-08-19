using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity StationApiKey (US-04a, ADR-005).</summary>
public class StationApiKeyConfiguration : IEntityTypeConfiguration<StationApiKey>
{
    public void Configure(EntityTypeBuilder<StationApiKey> builder)
    {
        builder.ToTable("StationApiKey");

        builder.HasKey(k => k.Id);

        // Độ dài cố định 64 ký tự hex (SHA-256), cùng quy ước với RefreshTokenConfiguration.TokenHash.
        builder.Property(k => k.KeyHash)
            .IsRequired()
            .HasMaxLength(64);

        // Ép lại Kind = Utc lúc đọc — xem UtcDateTimeValueConverter (sửa bug lệch giờ VN 19/08/2026).
        builder.Property(k => k.CreatedAtUtc)
            .IsRequired()
            .HasConversion(new UtcDateTimeValueConverter());

        builder.Property(k => k.RevokedAtUtc)
            .HasConversion(new NullableUtcDateTimeValueConverter());

        // AC5: hash phải là duy nhất trên toàn hệ thống để tra cứu ngược lại đúng 1 trạm khi xác thực.
        builder.HasIndex(k => k.KeyHash).IsUnique();

        // Tra cứu nhanh "key hiện tại/lịch sử key" theo trạm (GetCurrentAsync, AC2/AC4).
        builder.HasIndex(k => k.WorkStationId);

        // Không dùng khoá ngoại ở DB — WorkStationId là cột tham chiếu thuần.
    }
}
