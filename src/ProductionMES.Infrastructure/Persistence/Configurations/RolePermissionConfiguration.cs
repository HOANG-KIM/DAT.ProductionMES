using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration cho entity RolePermission (ADR-004) — bảng Admin chỉnh runtime qua UI
/// (cấp/thu hồi permission cho từng role).
/// </summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermission");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // 1 role chỉ được cấp 1 lần cho cùng 1 permission — API cấp/thu hồi (mục 5) idempotent dựa vào ràng
        // buộc này (cấp lại permission đã có -> không tạo thêm bản ghi, không lỗi).
        builder.HasIndex(rp => new { rp.Role, rp.PermissionId }).IsUnique();

        // Permission catalog thực tế không có API xóa, nhưng vẫn khai báo Cascade đúng theo ADR-004: xóa
        // Permission (nếu có, vd thao tác tay trên DB) thì xóa luôn RolePermission gắn theo, tránh orphan record.
        builder.HasOne(rp => rp.Permission)
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
