using System.Collections.Concurrent;
using System.Text;
using ProductionMES.Station.Tester.Serial;

namespace ProductionMES.Station.Tester.Tests.Fakes;

/// <summary>Cổng COM giả: giả lập máy thông điện ở đầu bên kia (tự trả PONG khi nhận PING, đẩy dòng vào luồng nhận, giả lập rút cáp).</summary>
public sealed class FakeSerialPort : ISerialPort
{
    private readonly BlockingCollection<byte[]> _incoming = new();
    private readonly object _lock = new();
    private readonly List<string> _written = new();
    private volatile bool _unplugged;
    private volatile bool _open;

    /// <summary>Có tự trả <c>#PONG,ST=IDLE,SEQ=0</c> cho mỗi <c>#PING</c> không (đặt false để giả lập máy ngừng trả lời).</summary>
    public volatile bool AutoPong = true;

    public bool IsOpen => _open && !_unplugged;

    /// <summary>Các dòng App đã ghi xuống cổng (không gồm '\n').</summary>
    public IReadOnlyList<string> Written
    {
        get
        {
            lock (_lock)
            {
                return _written.ToList();
            }
        }
    }

    public void Open() => _open = true;

    public void Close() => _open = false;

    /// <summary>Máy "gửi" 1 dòng cho App (tự thêm '\n' nếu thiếu).</summary>
    public void Receive(string text)
    {
        if (!text.EndsWith('\n'))
        {
            text += "\n";
        }

        _incoming.Add(Encoding.ASCII.GetBytes(text));
    }

    /// <summary>Giả lập rút cáp USB: Read/Write ném IOException.</summary>
    public void Unplug() => _unplugged = true;

    public int Read(byte[] buffer, int offset, int count)
    {
        if (_unplugged)
        {
            throw new IOException("Cổng đã bị rút.");
        }

        if (!_open)
        {
            throw new InvalidOperationException("Cổng chưa mở.");
        }

        if (_incoming.TryTake(out var data, 20))
        {
            Array.Copy(data, 0, buffer, offset, Math.Min(data.Length, count));
            return Math.Min(data.Length, count);
        }

        return 0;
    }

    public void Write(byte[] data, int offset, int count)
    {
        if (_unplugged)
        {
            throw new IOException("Cổng đã bị rút.");
        }

        var line = Encoding.ASCII.GetString(data, offset, count).TrimEnd('\n', '\r');
        lock (_lock)
        {
            _written.Add(line);
        }

        if (AutoPong && line == "#PING")
        {
            Receive("#PONG,ST=IDLE,SEQ=0");
        }
    }

    public void Dispose()
    {
        _open = false;
    }
}

/// <summary>Factory trả lần lượt các <see cref="FakeSerialPort"/>; cho phép giả lập "không mở được cổng" N lần đầu.</summary>
public sealed class FakeSerialPortFactory : ISerialPortFactory
{
    private int _openFailuresRemaining;
    private readonly List<FakeSerialPort> _created = new();
    private readonly object _lock = new();

    public FakeSerialPortFactory(int openFailures = 0)
    {
        _openFailuresRemaining = openFailures;
    }

    /// <summary>Hành động chạy lên từng port mới tạo (vd đặt AutoPong).</summary>
    public Action<FakeSerialPort>? Configure { get; set; }

    public IReadOnlyList<FakeSerialPort> Created
    {
        get
        {
            lock (_lock)
            {
                return _created.ToList();
            }
        }
    }

    public ISerialPort Create(TesterOptions options)
    {
        lock (_lock)
        {
            if (_openFailuresRemaining > 0)
            {
                _openFailuresRemaining--;
                throw new IOException("Không mở được cổng.");
            }

            var port = new FakeSerialPort();
            Configure?.Invoke(port);
            _created.Add(port);
            return port;
        }
    }
}
