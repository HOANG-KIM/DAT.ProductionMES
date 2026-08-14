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

        // Không dùng khoá ngoại ở DB — LineId là cột tham chiếu thuần (cùng lý do đã áp dụng cho
        // WorkStation.LineId). Line hiện chỉ soft-delete (Deactivate), chưa có API xóa cứng, nên chưa phát sinh
        // bản ghi BreakWindow mồ côi trong luồng nghiệp vụ hiện có.
        builder.HasIndex(b => b.LineId);
    }
}
