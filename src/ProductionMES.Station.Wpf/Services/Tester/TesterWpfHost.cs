using System.Windows;
using System.Windows.Threading;
using ProductionMES.Station.Tester;
using ProductionMES.Station.Tester.Station;
using Serilog;

namespace ProductionMES.Station.Wpf.Services.Tester;

/// <summary>
/// Đưa mọi chuyển trạng thái của <see cref="TesterStationController"/> về Dispatcher của UI — đảm bảo "mọi thay đổi trạng
/// thái xảy ra trên một luồng duy nhất" (spec-app 5.2). Luôn <c>BeginInvoke</c> (không chạy đồng bộ) để tránh vào lại.
/// </summary>
public sealed class WpfTesterDispatcher : ITesterDispatcher
{
    private readonly Dispatcher _dispatcher;

    public WpfTesterDispatcher()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public void Post(Action action)
    {
        if (_dispatcher.HasShutdownStarted)
        {
            return;
        }

        _dispatcher.BeginInvoke(action);
    }
}

/// <summary>Hẹn giờ timeout lệnh bằng <see cref="DispatcherTimer"/> (callback chạy trên UI thread).</summary>
public sealed class WpfTesterScheduler : ITesterScheduler
{
    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        var timer = new DispatcherTimer { Interval = delay };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            callback();
        };
        timer.Start();
        return new TimerHandle(timer);
    }

    private sealed class TimerHandle : IDisposable
    {
        private readonly DispatcherTimer _timer;

        public TimerHandle(DispatcherTimer timer) => _timer = timer;

        public void Dispose() => _timer.Stop();
    }
}

/// <summary>
/// Ghi log máy thông điện ra Serilog (file theo ngày, mức Debug — US-14 AC11: log serial vào/ra chỉ ghi FILE, không vào DB).
/// </summary>
public sealed class SerilogTesterLog : ITesterLog
{
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "Tester");

    public void Debug(string message) => Logger.Debug("{Message}", message);

    public void Info(string message) => Logger.Information("{Message}", message);

    public void Warn(string message) => Logger.Warning("{Message}", message);

    public void Error(string message, Exception? exception = null)
    {
        if (exception is null)
        {
            Logger.Error("{Message}", message);
        }
        else
        {
            Logger.Error(exception, "{Message}", message);
        }
    }
}
