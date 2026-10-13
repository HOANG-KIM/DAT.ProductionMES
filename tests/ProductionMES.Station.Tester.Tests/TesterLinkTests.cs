using System.Collections.Concurrent;
using ProductionMES.Station.Tester.Protocol;
using ProductionMES.Station.Tester.Tests.Fakes;

namespace ProductionMES.Station.Tester.Tests;

/// <summary>US-14 AC1-AC4, AC10, AC13 — TesterLink với cổng COM giả (thời gian rút ngắn để test chạy nhanh).</summary>
public class TesterLinkTests
{
    private static TesterOptions FastOptions() => new()
    {
        PortName = "FAKE",
        PingIntervalMs = 20,
        LinkTimeoutMs = 250,
        CommandTimeoutMs = 100,
        ReconnectIntervalMs = 30,
    };

    private static bool WaitFor(Func<bool> condition, int timeoutMs = 3000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return condition();
    }

    // AC1/AC13: mở cổng, gửi #HELLO? và PING định kỳ.
    [Fact]
    public void Start_MoCongGuiHelloQueryVaPingDinhKy()
    {
        var factory = new FakeSerialPortFactory();
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        var opened = new ManualResetEventSlim();
        link.PortOpened += opened.Set;

        link.Start();

        Assert.True(opened.Wait(2000));
        var port = factory.Created[0];
        Assert.True(WaitFor(() => port.Written.Count(l => l == "#PING") >= 3));
        Assert.Equal("#HELLO?", port.Written[0]);
        Assert.True(link.IsPortOpen);
    }

    // AC3: PONG được chuyển tiếp cho người nghe.
    [Fact]
    public void Start_NhanPong_RaiseMessageReceived()
    {
        var factory = new FakeSerialPortFactory();
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        var messages = new ConcurrentQueue<TesterMessage>();
        link.MessageReceived += messages.Enqueue;

        link.Start();

        Assert.True(WaitFor(() => messages.OfType<PongMessage>().Any()));
    }

    // AC10: dòng log không phải '#', dòng hỏng không làm sập, dòng hợp lệ sau đó vẫn nhận được.
    [Fact]
    public void Nhan_DongRacVaDongHong_BoQuaVaTiepTucNhanDongHopLe()
    {
        var factory = new FakeSerialPortFactory { Configure = p => p.AutoPong = true };
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        var messages = new ConcurrentQueue<TesterMessage>();
        link.MessageReceived += messages.Enqueue;
        link.Start();
        Assert.True(WaitFor(() => factory.Created.Count == 1));
        var port = factory.Created[0];

        port.Receive("ets Jun  8 2016 00:22:57");
        port.Receive("#RESULT,SEQ=5,R=OK");
        port.Receive("#RESULT,SEQ=5,R=OK,GM=3A7F,FUTURE=1");

        Assert.True(WaitFor(() => messages.OfType<ResultMessage>().Any()));
        var result = Assert.Single(messages.OfType<ResultMessage>());
        Assert.Equal(5, result.Seq);
    }

    // AC3/AC4: mất PONG quá LinkTimeout -> đóng cổng, báo PortClosed, thử mở lại.
    [Fact]
    public void MatPong_DongCongBaoPortClosedVaThuMoLai()
    {
        var factory = new FakeSerialPortFactory();
        var firstPortSilenced = false;
        factory.Configure = p =>
        {
            if (!firstPortSilenced)
            {
                firstPortSilenced = true;
                p.AutoPong = false;
            }
        };
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        var closedReasons = new ConcurrentQueue<string>();
        var openedCount = 0;
        link.PortClosed += closedReasons.Enqueue;
        link.PortOpened += () => Interlocked.Increment(ref openedCount);

        link.Start();

        Assert.True(WaitFor(() => !closedReasons.IsEmpty), "Phải báo PortClosed khi mất PONG.");
        Assert.True(WaitFor(() => Volatile.Read(ref openedCount) >= 2), "Phải tự mở lại cổng.");
        Assert.True(WaitFor(() => factory.Created.Count >= 2));
    }

    // AC4: rút cáp (lỗi I/O) -> đóng + mở lại khi cắm lại.
    [Fact]
    public void RutCap_BaoPortClosedRoiMoLai()
    {
        var factory = new FakeSerialPortFactory();
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        var closed = new ManualResetEventSlim();
        var openedCount = 0;
        link.PortClosed += _ => closed.Set();
        link.PortOpened += () => Interlocked.Increment(ref openedCount);
        link.Start();
        Assert.True(WaitFor(() => factory.Created.Count == 1));

        factory.Created[0].Unplug();

        Assert.True(closed.Wait(3000));
        Assert.True(WaitFor(() => Volatile.Read(ref openedCount) >= 2));
    }

    // AC4: cổng chưa cắm -> thử mở lại mỗi ReconnectInterval cho tới khi mở được, không cần khởi động lại.
    [Fact]
    public void CongChuaCam_ThuMoLaiChoTiMoDuoc()
    {
        var factory = new FakeSerialPortFactory(openFailures: 3);
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        var opened = new ManualResetEventSlim();
        link.PortOpened += opened.Set;

        link.Start();

        Assert.True(opened.Wait(3000));
        Assert.Single(factory.Created);
    }

    // AC2/US-11 AC7: Stop đóng cổng, ngừng PING, KHÔNG raise PortClosed, không tự mở lại.
    [Fact]
    public void Stop_DongCongNgungPingKhongTuMoLai()
    {
        var factory = new FakeSerialPortFactory();
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        var closedRaised = false;
        link.PortClosed += _ => closedRaised = true;
        link.Start();
        Assert.True(WaitFor(() => factory.Created.Count == 1 && factory.Created[0].Written.Count > 2));

        link.Stop();
        var writtenAfterStop = factory.Created[0].Written.Count;
        Thread.Sleep(150);

        Assert.False(link.IsPortOpen);
        Assert.False(closedRaised);
        Assert.Equal(writtenAfterStop, factory.Created[0].Written.Count);
        Assert.Single(factory.Created);
    }

    // US-11 AC8: bật lại sau khi tắt -> mở lại cổng.
    [Fact]
    public void StartSauKhiStop_MoLaiCong()
    {
        var factory = new FakeSerialPortFactory();
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        link.Start();
        Assert.True(WaitFor(() => factory.Created.Count == 1));
        link.Stop();

        link.Start();

        Assert.True(WaitFor(() => factory.Created.Count == 2));
        Assert.True(WaitFor(() => link.IsPortOpen));
    }

    // Send khi chưa mở cổng -> false, không ném.
    [Fact]
    public void Send_KhiCongChuaMo_TraFalse()
    {
        var factory = new FakeSerialPortFactory();
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);

        Assert.False(link.Send(TesterCommand.Bind(1)));
    }

    // AC13: lệnh gửi xuống đúng định dạng.
    [Fact]
    public void Send_GhiDungDong()
    {
        var factory = new FakeSerialPortFactory();
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        link.Start();
        Assert.True(WaitFor(() => link.IsPortOpen));

        Assert.True(link.Send(TesterCommand.Bind(42)));

        Assert.True(WaitFor(() => factory.Created[0].Written.Contains("#BIND,SEQ=42")));
    }

    // Handler của người dùng ném exception không làm sập link.
    [Fact]
    public void HandlerNemException_LinkVanChayTiep()
    {
        var factory = new FakeSerialPortFactory();
        using var link = new TesterLink(FastOptions(), factory, NullTesterLog.Instance);
        var count = 0;
        link.MessageReceived += _ =>
        {
            Interlocked.Increment(ref count);
            throw new InvalidOperationException("boom");
        };

        link.Start();

        Assert.True(WaitFor(() => Volatile.Read(ref count) >= 3));
        Assert.True(link.IsPortOpen);
    }
}
