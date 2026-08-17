using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration cho entity LineStageSequence (FR-03/US-03). Ràng buộc unique ở DB đóng vai trò lưới
/// an toàn bổ sung cho validate đã thực hiện ở Service (AC4 — không trùng số thứ tự; AC5 — không trùng công
/// đoạn trong cùng Line, đảm bảo không thể tạo vòng lặp).
/// </summary>
public class LineStageSequenceConfiguration : IEntityTypeConfiguration<LineStageSequence>
{
    public void Configure(EntityTypeBuilder<LineStageSequence> builder)
    {
        builder.ToTable("LineStageSequence");

        builder.HasKey(k => k.Id);

        // Không dùng khoá ngoại ở DB (LineId/StageId là cột tham chiếu thuần) — toàn vẹn quan hệ xử lý ở Service
        // (cùng nguyên tắc đã áp dụng cho toàn bộ các entity khác, xem CLAUDE.md).

        // AC4: không trùng số thứ tự trong cùng 1 Line.
        builder.HasIndex(k => new { k.LineId, k.SequenceNumber }).IsUnique();

        // AC5: không trùng công đoạn trong cùng 1 Line — điều kiện cấu trúc đảm bảo không có vòng lặp.
        builder.HasIndex(k => new { k.LineId, k.StageId }).IsUnique();
    }
}
