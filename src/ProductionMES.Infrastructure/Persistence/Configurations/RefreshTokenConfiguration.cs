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

        // RefreshToken là bản ghi phụ thuộc (owned-like) của User, không phải danh mục dùng chung như
        // Line/Stage — xóa User thì các RefreshToken gắn theo cũng không còn ý nghĩa, nên Cascade (khác với
        // Restrict dùng cho FK trỏ tới danh mục master data ở các Configuration khác trong project).
        builder.HasOne(k => k.User)
            .WithMany()
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
