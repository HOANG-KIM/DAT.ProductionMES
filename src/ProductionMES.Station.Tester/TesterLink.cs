using System.Text;
using ProductionMES.Station.Tester.Protocol;
using ProductionMES.Station.Tester.Serial;

namespace ProductionMES.Station.Tester;

/// <summary>
/// Đường truyền tới máy thông điện (US-14): mở cổng, đọc dòng ở luồng nền, PING định kỳ, phát hiện mất kết nối, tự mở
/// lại cổng. Mọi sự kiện được raise từ luồng nền của link — nơi nhận (<see cref="TesterStationController"/>) tự
/// chuyển sang luồng xử lý duy nhất (Dispatcher).
/// </summary>
public interface ITesterLink : IDisposable
{
    /// <summary>Cổng vừa mở thành công (CHƯA chắc máy trả lời — chờ PONG).</summary>
    event Action? PortOpened;

    /// <summary>Cổng đã đóng do lỗi I/O / mất PONG quá <see cref="TesterOptions.LinkTimeoutMs"/> (tham số: lý do). KHÔNG raise khi gọi <see cref="Stop"/> chủ động.</summary>
    event Action<string>? PortClosed;

    /// <summary>Một dòng giao thức hợp lệ (kể cả PONG) vừa nhận.</summary>
    event Action<TesterMessage>? MessageReceived;

    bool IsPortOpen { get; }

    /// <summary>Bắt đầu mở cổng + PING + tự kết nối lại. Gọi nhiều lần không có tác dụng thêm.</summary>
    void Start();

    /// <summary>Đóng cổng, ngừng PING và ngừng tự mở lại (khi công tắc TẮT — US-14 AC2, US-11 AC7). Idempotent.</summary>
    void Stop();

    /// <summary>Gửi 1 lệnh (ghi có khóa để PING và lệnh khác không chen nhau). Trả false nếu cổng không mở / ghi lỗi.</summary>
    bool Send(TesterCommand command);
}

/// <inheritdoc cref="ITesterLink"/>
public sealed class TesterLink : ITesterLink
{
    private readonly TesterOptions _options;
    private readonly ISerialPortFactory _portFactory;
    private readonly ITesterLog _log;
    private readonly object _writeLock = new();
    private readonly object _lifecycleLock = new();
    private readonly ManualResetEventSlim _wake = new(false);
    private readonly LineAssembler _assembler = new();

    private CancellationTokenSource? _cts;
    private Thread? _thread;
    private volatile ISerialPort? _port;
    private long _lastPongTicks;
    private volatile string? _ioError;

    public TesterLink(TesterOptions options, ISerialPortFactory portFactory, ITesterLog log)
    {
        _options = options;
        _portFactory = portFactory;
        _log = log;
    }

    public event Action? PortOpened;

    public event Action<string>? PortClosed;

    public event Action<TesterMessage>? MessageReceived;

    public bool IsPortOpen => _port is { IsOpen: true };

    public void Start()
    {
        lock (_lifecycleLock)
        {
            if (_thread is not null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _thread = new Thread(() => RunLoop(token)) { IsBackground = true, Name = "TesterLink" };
            _thread.Start();
        }
    }

    public void Stop()
    {
        Thread? thread;
        CancellationTokenSource? cts;
        lock (_lifecycleLock)
        {
            thread = _thread;
            cts = _cts;
            _thread = null;
            _cts = null;
        }

        if (thread is null)
        {
            return;
        }

        cts!.Cancel();
        _wake.Set();
        if (Thread.CurrentThread != thread)
        {
            thread.Join(TimeSpan.FromSeconds(3));
        }

        cts.Dispose();
    }

    public bool Send(TesterCommand command)
    {
        var port = _port;
        if (port is null || !port.IsOpen)
        {
            return false;
        }

        var line = command.ToLine();
        var bytes = Encoding.ASCII.GetBytes(line + "\n");
        try
        {
            lock (_writeLock)
            {
                port.Write(bytes, 0, bytes.Length);
            }

            _log.Debug($"TX: {line}");
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException or TimeoutException)
        {
            _log.Warn($"Ghi cổng COM lỗi: {ex.Message}");
            _ioError ??= ex.Message;
            _wake.Set();
            return false;
        }
    }

    public void Dispose()
    {
        Stop();
        _wake.Dispose();
    }

    private void RunLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            ISerialPort? port = null;
            try
            {
                port = _portFactory.Create(_options);
                port.Open();
            }
            catch (Exception ex)
            {
                _log.Debug($"Không mở được cổng '{_options.PortName}': {ex.Message}");
                SafeDispose(port);
                if (WaitOrCancelled(_options.ReconnectIntervalMs, ct))
                {
                    break;
                }

                continue;
            }

            _log.Info($"Đã mở cổng {_options.PortName} @ {_options.BaudRate} 8N1 (DTR/RTS tắt).");
            _ioError = null;
            _wake.Reset();
            _assembler.Reset();
            Interlocked.Exchange(ref _lastPongTicks, Environment.TickCount64);
            _port = port;

            using var readerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var reader = new Thread(() => ReadLoop(port, readerCts.Token)) { IsBackground = true, Name = "TesterLink.Reader" };
            reader.Start();

            Raise(() => PortOpened?.Invoke());
            Send(TesterCommand.HelloQuery());

            var reason = RunSession(ct);

            _port = null;
            readerCts.Cancel();
            SafeDispose(port);
            reader.Join(TimeSpan.FromSeconds(2));

            if (ct.IsCancellationRequested)
            {
                break;
            }

            _log.Warn($"Mất kết nối máy thông điện: {reason}");
            Raise(() => PortClosed?.Invoke(reason));

            if (WaitOrCancelled(_options.ReconnectIntervalMs, ct))
            {
                break;
            }
        }

        _port = null;
    }

    /// <summary>PING định kỳ cho tới khi mất PONG / lỗi I/O / bị hủy. Trả về lý do kết thúc phiên.</summary>
    private string RunSession(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var ioError = _ioError;
            if (ioError is not null)
            {
                return ioError;
            }

            var sincePong = Environment.TickCount64 - Interlocked.Read(ref _lastPongTicks);
            if (sincePong > _options.LinkTimeoutMs)
            {
                return $"Không nhận PONG trong {_options.LinkTimeoutMs} ms.";
            }

            Send(TesterCommand.Ping());

            // Ngủ tới chu kỳ PING kế tiếp, nhưng dậy ngay nếu bị hủy hoặc reader/writer báo lỗi.
            WaitHandle.WaitAny(new[] { ct.WaitHandle, _wake.WaitHandle }, _options.PingIntervalMs);
        }

        return "Đã dừng.";
    }

    private void ReadLoop(ISerialPort port, CancellationToken ct)
    {
        var buffer = new byte[512];
        while (!ct.IsCancellationRequested)
        {
            int read;
            try
            {
                read = port.Read(buffer, 0, buffer.Length);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _log.Warn($"Đọc cổng COM lỗi: {ex.Message}");
                _ioError ??= ex.Message;
                _wake.Set();
                return;
            }
            catch
            {
                return;
            }

            if (read > 0)
            {
                _assembler.Feed(buffer, 0, read, OnLine, () => _log.Warn("Dòng nhận được dài quá 256 byte, đã bỏ."));
            }
        }
    }

    private void OnLine(string line)
    {
        var result = ProtocolParser.Parse(line);
        switch (result.Status)
        {
            case ParseStatus.NotProtocol:
                // Log debug của firmware / rác bootloader: chỉ ghi file, không xử lý.
                _log.Debug($"RX(log): {line}");
                return;

            case ParseStatus.Invalid:
                _log.Warn($"RX(hỏng): {line} -> {result.Error}");
                return;
        }

        _log.Debug($"RX: {line}");
        var message = result.Message!;
        if (message is PongMessage)
        {
            Interlocked.Exchange(ref _lastPongTicks, Environment.TickCount64);
        }

        Raise(() => MessageReceived?.Invoke(message));
    }

    /// <summary>Handler của người dùng lỗi không được làm sập luồng đọc / luồng link.</summary>
    private void Raise(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _log.Error("Lỗi trong handler sự kiện của TesterLink.", ex);
        }
    }

    private static bool WaitOrCancelled(int milliseconds, CancellationToken ct)
        => ct.WaitHandle.WaitOne(Math.Max(1, milliseconds));

    private static void SafeDispose(ISerialPort? port)
    {
        try
        {
            port?.Dispose();
        }
        catch
        {
            // Cổng đã hỏng: bỏ qua.
        }
    }
}
