using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>Fluent API configuration cho entity BreakWindow (FR-01/FR-09a/US-01a).</summary>
public class BreakWindowConfiguration : IEntityTypeConfiguration<BreakWindow>
{
    public void Configure(EntityTypeBuilder<BreakWindow> builder)
    {
        builder.ToTable("BreakWindow");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.StartTime)
            .IsRequired();

        builder.Property(b => b.EndTime)
            .IsRequired();

        builder.Property(b => b.Note)
            .HasMaxLength(500);

        // FK thuần (không cấu hình navigation property 2 chiều — cùng lý do đã áp dụng cho WorkStation.LineId).
        // Cascade: xóa Line xóa luôn khung giờ nghỉ của Line đó — BreakWindow không có ý nghĩa lịch sử độc lập
        // ngoài phạm vi Line sở hữu nó (khác Scan/RefreshToken, vốn cần giữ lại dù entity chủ bị vô hiệu hóa).
        // Thực tế Line hiện tại chỉ soft-delete (Deactivate), chưa có API xóa cứng, nên Cascade chỉ có hiệu lực
        // phòng hờ; giữ Restrict sẽ không đúng vì Line không có API xóa cứng để va phải ràng buộc này.
        builder.HasOne<Line>()
            .WithMany()
            .HasForeignKey(b => b.LineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
