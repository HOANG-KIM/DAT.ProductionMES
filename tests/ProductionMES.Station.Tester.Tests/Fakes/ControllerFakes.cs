using ProductionMES.Station.Tester.Protocol;
using ProductionMES.Station.Tester.Station;

namespace ProductionMES.Station.Tester.Tests.Fakes;

/// <summary>Link giả: ghi lại lệnh đã gửi, cho test bơm sự kiện/dòng giao thức vào controller.</summary>
public sealed class FakeTesterLink : ITesterLink
{
    public List<TesterCommand> Sent { get; } = new();

    public bool Started { get; private set; }

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public bool IsPortOpen { get; set; }

    /// <summary>Giả lập ghi lỗi (cổng không mở).</summary>
    public bool SendFails { get; set; }

    public event Action? PortOpened;

    public event Action<string>? PortClosed;

    public event Action<TesterMessage>? MessageReceived;

    public void Start()
    {
        Started = true;
        StartCount++;
    }

    public void Stop()
    {
        Started = false;
        IsPortOpen = false;
        StopCount++;
    }

    public bool Send(TesterCommand command)
    {
        if (SendFails)
        {
            return false;
        }

        Sent.Add(command);
        return true;
    }

    public void Dispose()
    {
    }

    public void OpenPort()
    {
        IsPortOpen = true;
        PortOpened?.Invoke();
    }

    public void ClosePort(string reason = "mất kết nối")
    {
        IsPortOpen = false;
        PortClosed?.Invoke(reason);
    }

    /// <summary>Máy gửi 1 dòng giao thức (phải parse được).</summary>
    public void Receive(string line)
    {
        var parsed = ProtocolParser.Parse(line);
        Assert.Equal(ParseStatus.Ok, parsed.Status);
        MessageReceived?.Invoke(parsed.Message!);
    }

    public IEnumerable<string> SentLines => Sent.Select(c => c.ToLine());

    public int CountSent(TesterCommandKind kind) => Sent.Count(c => c.Kind == kind);
}

public sealed class ImmediateDispatcher : ITesterDispatcher
{
    public void Post(Action action) => action();
}

/// <summary>Hẹn giờ thủ công: test tự "bấm" timeout bằng <see cref="FireAll"/>.</summary>
public sealed class ManualScheduler : ITesterScheduler
{
    private sealed class Entry : IDisposable
    {
        public Entry(Action callback) => Callback = callback;

        public Action Callback { get; }

        public bool Cancelled { get; set; }

        public void Dispose() => Cancelled = true;
    }

    private readonly List<Entry> _entries = new();

    public int ActiveCount => _entries.Count(e => !e.Cancelled);

    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        var entry = new Entry(callback);
        _entries.Add(entry);
        return entry;
    }

    /// <summary>Kích hoạt mọi timeout đang chờ (chưa bị hủy) đúng 1 lượt.</summary>
    public void FireAll()
    {
        foreach (var entry in _entries.Where(e => !e.Cancelled).ToList())
        {
            entry.Cancelled = true;
            entry.Callback();
        }
    }
}

public sealed class FakeServerApi : ITesterServerApi
{
    public Func<string, Task<TagCheckResult>> OnCheck { get; set; } = _ => Task.FromResult(new TagCheckResult(true, Payload: "check-payload"));

    public Func<TesterScanSave, Task<TesterSaveResult>> OnSave { get; set; } = _ => Task.FromResult(new TesterSaveResult(true, Payload: "save-payload"));

    public Func<TesterEvent, Task> OnEvent { get; set; } = _ => Task.CompletedTask;

    public List<string> Checks { get; } = new();

    public List<TesterScanSave> Saves { get; } = new();

    public List<TesterEvent> Events { get; } = new();

    public Task<TagCheckResult> CheckTagAsync(string tagCode, CancellationToken cancellationToken)
    {
        Checks.Add(tagCode);
        return OnCheck(tagCode);
    }

    public Task<TesterSaveResult> SaveTesterScanAsync(TesterScanSave save, CancellationToken cancellationToken)
    {
        Saves.Add(save);
        return OnSave(save);
    }

    public Task RecordEventAsync(TesterEvent testerEvent, CancellationToken cancellationToken)
    {
        Events.Add(testerEvent);
        return OnEvent(testerEvent);
    }
}
