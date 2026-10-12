using System.IO.Ports;

namespace ProductionMES.Station.Tester.Serial;

/// <summary>
/// Cổng COM thật qua <c>System.IO.Ports</c> (US-14 AC1): 115200 8N1, <c>Handshake.None</c>,
/// <b><c>DtrEnable = false</c>, <c>RtsEnable = false</c></b> — DTR/RTS của cổng UART trên DevKitC-1 nối vào mạch auto-reset
/// (EN/IO0) của ESP32, bật lên là máy bị reset. Các thuộc tính này được đặt TRƯỚC khi <see cref="Open"/>.
/// </summary>
/// <remarks>
/// Ghi chú vận hành: một số driver USB-UART vẫn kéo DTR trong thoáng chốc giữa lúc mở handle và lúc áp DCB; nếu máy
/// bị reset đúng lúc App mở cổng, thì đó là hành vi driver, không phải lỗi logic — firmware vẫn phục hồi được (gửi
/// HELLO lúc boot, App đồng bộ theo BOOT, US-14 AC5).
/// </remarks>
public sealed class SystemSerialPort : ISerialPort
{
    private const int ReadTimeoutMs = 200;
    private const int WriteTimeoutMs = 1000;

    private readonly SerialPort _port;

    public SystemSerialPort(TesterOptions options)
    {
        _port = new SerialPort(options.PortName, options.BaudRate, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            DtrEnable = false,
            RtsEnable = false,
            ReadTimeout = ReadTimeoutMs,
            WriteTimeout = WriteTimeoutMs,
            Encoding = System.Text.Encoding.ASCII,
        };
    }

    public bool IsOpen => _port.IsOpen;

    public void Open() => _port.Open();

    public void Close()
    {
        try
        {
            if (_port.IsOpen)
            {
                _port.Close();
            }
        }
        catch (IOException)
        {
            // Cổng đã bị rút: đóng coi như xong.
        }
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        try
        {
            return _port.Read(buffer, offset, count);
        }
        catch (TimeoutException)
        {
            return 0;
        }
    }

    public void Write(byte[] data, int offset, int count) => _port.Write(data, offset, count);

    public void Dispose()
    {
        Close();
        _port.Dispose();
    }
}

/// <summary>Factory tạo <see cref="SystemSerialPort"/>.</summary>
public sealed class SystemSerialPortFactory : ISerialPortFactory
{
    public ISerialPort Create(TesterOptions options) => new SystemSerialPort(options);
}
