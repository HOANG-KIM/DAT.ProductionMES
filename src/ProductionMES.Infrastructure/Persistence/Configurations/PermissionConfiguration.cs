using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity Permission (ADR-004) — catalog cố định (Resource, Action).</summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permission");

        builder.HasKey(p => p.Id);

        // Lưu dạng chuỗi (vd "Line", "View") thay vì số nguyên — cùng lý do đã áp dụng cho User.UserRole:
        // dễ đọc/truy vấn trực tiếp trên DB, ổn định hơn nếu thứ tự khai báo enum thay đổi sau này.
        builder.Property(p => p.Resource)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Action)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Catalog cố định — không cho phép 2 bản ghi cùng cặp (Resource, Action).
        builder.HasIndex(p => new { p.Resource, p.Action }).IsUnique();
    }
}
