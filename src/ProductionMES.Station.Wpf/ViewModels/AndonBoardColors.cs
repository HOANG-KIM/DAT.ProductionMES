using System.Windows.Media;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// Helper màu dùng chung cho <see cref="AndonBoardViewModel"/> và <see cref="AndonBoardRowViewModel"/> (US-09
/// AC2/AC3) — tránh trùng lặp logic "dương xanh/âm đỏ" ở 2 nơi (chỉ số tổng ở Row 0 và từng dòng trong bảng).
/// </summary>
internal static class AndonBoardColors
{
    private static readonly Brush PositiveBrush = FreezeBrush("#4CAF50");
    private static readonly Brush NegativeBrush = FreezeBrush("#E53935");
    private static readonly Brush NeutralBrush = Brushes.White;

    /// <summary>AC2 — dương: xanh. AC3 — âm: đỏ. Bằng 0 (không thuộc AC nào): trắng trung tính.</summary>
    public static Brush GetBalanceBrush(int balance) => balance switch
    {
        > 0 => PositiveBrush,
        < 0 => NegativeBrush,
        _ => NeutralBrush,
    };

    private static Brush FreezeBrush(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }
}
