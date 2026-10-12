using System.Collections.Concurrent;
using System.Text;
using ProductionMES.Station.Tester.Serial;
using ProductionMES.Station.Tester.Station;
using ProductionMES.Tester.Simulator;

namespace ProductionMES.Station.Tester.Tests.Fakes;

/// <summary>
/// Cổng COM giả nối thẳng vào <see cref="MachineSimulator"/> (firmware giả lập theo spec): App ghi dòng -> simulator xử lý,
/// simulator "gửi" dòng -> vào luồng đọc của App.
/// </summary>
public sealed class SimulatorSerialPort : ISerialPort
{
    private readonly MachineSimulator _simulator;
    private readonly BlockingCollection<byte[]> _incoming = new();
    private readonly Action<string> _onSimulatorLine;
    private readonly StringBuilder _partial = new();
    private volatile bool _open;
    private volatile bool _unplugged;

    public SimulatorSerialPort(MachineSimulator simulator)
    {
        _simulator = simulator;
        _onSimulatorLine = line =>
        {
            if (!_unplugged)
            {
                _incoming.Add(Encoding.ASCII.GetBytes(line + "\n"));
            }
        };
    }

    public bool IsOpen => _open && !_unplugged;

    public void Open()
    {
        _simulator.Send += _onSimulatorLine;
        _open = true;
    }

    public void Close()
    {
        _open = false;
        _simulator.Send -= _onSimulatorLine;
    }

    public void Unplug() => _unplugged = true;

    public int Read(byte[] buffer, int offset, int count)
    {
        if (_unplugged)
        {
            throw new IOException("Cổng đã bị rút.");
        }

        if (_incoming.TryTake(out var data, 20))
        {
            var n = Math.Min(data.Length, count);
            Array.Copy(data, 0, buffer, offset, n);
            return n;
        }

        return 0;
    }

    public void Write(byte[] data, int offset, int count)
    {
        if (_unplugged)
        {
            throw new IOException("Cổng đã bị rút.");
        }

        _partial.Append(Encoding.ASCII.GetString(data, offset, count));
        while (true)
        {
            var text = _partial.ToString();
            var nl = text.IndexOf('\n');
            if (nl < 0)
            {
                break;
            }

            var line = text[..nl];
            _partial.Remove(0, nl + 1);
            _simulator.HandleLine(line);
        }
    }

    public void Dispose() => Close();
}

public sealed class SimulatorPortFactory : ISerialPortFactory
{
    private readonly MachineSimulator _simulator;

    public SimulatorPortFactory(MachineSimulator simulator) => _simulator = simulator;

    /// <summary>false = cáp USB đang rút: mở cổng thất bại.</summary>
    public volatile bool Plugged = true;

    public SimulatorSerialPort? Current { get; private set; }

    public ISerialPort Create(TesterOptions options)
    {
        if (!Plugged)
        {
            throw new IOException("Không thấy cổng COM (cáp đã rút).");
        }

        Current = new SimulatorSerialPort(_simulator);
        return Current;
    }

    /// <summary>Giả lập rút cáp: cổng đang mở báo lỗi I/O, các lần mở lại thất bại cho tới khi <see cref="Replug"/>.</summary>
    public void Unplug()
    {
        Plugged = false;
        Current?.Unplug();
    }

    public void Replug() => Plugged = true;
}

/// <summary>
/// "UI thread" giả: 1 luồng riêng có <see cref="SynchronizationContext"/> — mọi continuation sau <c>await</c> bắt đầu từ luồng
/// này quay về chính nó, y hệt Dispatcher của WPF. Dùng để test tích hợp không dính race của controller (không thread-safe).
/// </summary>
public sealed class SingleThreadDispatcher : ITesterDispatcher, IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    public SingleThreadDispatcher()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "TestDispatcher" };
        _thread.Start();
    }

    public void Post(Action action)
    {
        if (!_queue.IsAddingCompleted)
        {
            _queue.Add(action);
        }
    }

    /// <summary>Chạy <paramref name="func"/> trên luồng dispatcher, chờ kết quả.</summary>
    public T Invoke<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task.GetAwaiter().GetResult();
    }

    public void Invoke(Action action) => Invoke<object?>(() =>
    {
        action();
        return null;
    });

    /// <summary>Chạy hàm async trên luồng dispatcher (continuation cũng về luồng này), chờ hoàn tất.</summary>
    public async Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(async () =>
        {
            try
            {
                tcs.SetResult(await func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return await tcs.Task;
    }

    private void Run()
    {
        SynchronizationContext.SetSynchronizationContext(new QueueContext(this));
        foreach (var action in _queue.GetConsumingEnumerable())
        {
            try
            {
                action();
            }
            catch
            {
                // Lỗi trong action của test không được làm sập luồng.
            }
        }
    }

    public void Dispose() => _queue.CompleteAdding();

    private sealed class QueueContext : SynchronizationContext
    {
        private readonly SingleThreadDispatcher _owner;

        public QueueContext(SingleThreadDispatcher owner) => _owner = owner;

        public override void Post(SendOrPostCallback d, object? state) => _owner.Post(() => d(state));

        public override void Send(SendOrPostCallback d, object? state) => _owner.Invoke(() => d(state));
    }
}

/// <summary>Hẹn giờ thật (System.Threading.Timer) nhưng callback được đưa về luồng dispatcher.</summary>
public sealed class ThreadTimerScheduler : ITesterScheduler
{
    private readonly SingleThreadDispatcher _dispatcher;

    public ThreadTimerScheduler(SingleThreadDispatcher dispatcher) => _dispatcher = dispatcher;

    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        var cancelled = false;
        var timer = new Timer(_ => _dispatcher.Post(() =>
        {
            if (!Volatile.Read(ref cancelled))
            {
                callback();
            }
        }), null, delay, Timeout.InfiniteTimeSpan);
        return new Handle(() =>
        {
            Volatile.Write(ref cancelled, true);
            timer.Dispose();
        });
    }

    private sealed class Handle : IDisposable
    {
        private readonly Action _dispose;

        public Handle(Action dispose) => _dispose = dispose;

        public void Dispose() => _dispose();
    }
}
