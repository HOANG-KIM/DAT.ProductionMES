namespace ProductionMES.Station.Tester.Serial;

/// <summary>
/// Abstraction cổng COM để <see cref="TesterLink"/> test được không cần phần cứng. Mọi lỗi I/O (cổng bị rút, driver
/// báo lỗi) phải được ném ra dưới dạng <see cref="IOException"/> / <see cref="InvalidOperationException"/> /
/// <see cref="UnauthorizedAccessException"/> để link đóng cổng và thử mở lại.
/// </summary>
public interface ISerialPort : IDisposable
{
    bool IsOpen { get; }

    /// <summary>Mở cổng. Ném exception nếu không mở được (cổng không tồn tại, đang bị app khác giữ...).</summary>
    void Open();

    void Close();

    /// <summary>Đọc tối đa <paramref name="count"/> byte, chờ tối đa khoảng ngắn (~200 ms); trả 0 nếu hết thời gian chờ mà không có dữ liệu.</summary>
    int Read(byte[] buffer, int offset, int count);

    void Write(byte[] data, int offset, int count);
}

/// <summary>Tạo <see cref="ISerialPort"/> theo cấu hình (mỗi lần mở lại cổng tạo 1 instance mới).</summary>
public interface ISerialPortFactory
{
    ISerialPort Create(TesterOptions options);
}
