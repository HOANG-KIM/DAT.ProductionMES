using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity RefreshToken (ADR-003).</summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        // Dùng để tìm bản ghi theo hash khi refresh/logout — phải unique vì hash SHA-256 gần như không đụng độ.
        builder.HasIndex(k => k.TokenHash).IsUnique();

        builder.Property(k => k.ReplacedByTokenHash)
            .HasMaxLength(128);

        // Ép lại Kind = Utc lúc đọc — xem UtcDateTimeValueConverter (sửa bug lệch giờ VN 19/08/2026).
        builder.Property(k => k.ExpiresAtUtc)
            .HasConversion(new UtcDateTimeValueConverter());

        builder.Property(k => k.CreatedAtUtc)
            .HasConversion(new UtcDateTimeValueConverter());

        builder.Property(k => k.RevokedAtUtc)
            .HasConversion(new NullableUtcDateTimeValueConverter());

        // Không dùng khoá ngoại ở DB — UserId là cột tham chiếu thuần. Tra cứu nhanh "các refresh token đang
        // hoạt động của 1 User" khi phát hiện reuse token đã revoke (AuthService.RefreshAsync).
        builder.HasIndex(k => k.UserId);
    }
}
