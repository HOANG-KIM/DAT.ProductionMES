using ProductionMES.Station.Tester.Station;
using ProductionMES.Station.Tester.Tests.Fakes;
using ProductionMES.Tester.Simulator;

namespace ProductionMES.Station.Tester.Tests;

/// <summary>
/// Tích hợp TesterLink + TesterStationController + firmware giả lập (<see cref="MachineSimulator"/>, theo spec mục 4-5): chạy
/// các kịch bản nghiệm thu S2-S11 (spec-app 12.3) end-to-end ở mức giao thức, không cần phần cứng.
/// </summary>
public sealed class TesterIntegrationTests : IDisposable
{
    private readonly MachineSimulator _machine;
    private readonly SimulatorPortFactory _factory;
    private readonly SingleThreadDispatcher _dispatcher = new();
    private readonly FakeServerApi _api = new();
    private readonly TesterLink _link;
    private readonly TesterStationController _controller;
    private readonly List<TesterNotice> _notices = new();
    private long _simClockMs = 1_000_000;

    public TesterIntegrationTests()
    {
        _machine = new MachineSimulator(() => Interlocked.Read(ref _simClockMs));
        _factory = new SimulatorPortFactory(_machine);
        var options = new TesterOptions
        {
            PortName = "SIM",
            PingIntervalMs = 20,
            LinkTimeoutMs = 400,
            CommandTimeoutMs = 300,
            ReconnectIntervalMs = 30,
        };
        _link = new TesterLink(options, _factory, NullTesterLog.Instance);
        _controller = new TesterStationController(
            _link, _api, _dispatcher, new ThreadTimerScheduler(_dispatcher), options, NullTesterLog.Instance);
        _controller.NoticeRaised += n => _notices.Add(n);
    }

    public void Dispose()
    {
        _dispatcher.Invoke(() => _controller.Dispose());
        _dispatcher.Dispose();
    }

    private TesterState State => _dispatcher.Invoke(() => _controller.State);

    private bool WaitFor(Func<bool> condition, int timeoutMs = 4000)
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

    private bool WaitState(TesterState expected) => WaitFor(() => State == expected);

    private List<TesterNotice> Notices() => _dispatcher.Invoke(() => _notices.ToList());

    private List<TesterEvent> Events() => _dispatcher.Invoke(() => _api.Events.ToList());

    private List<TesterScanSave> Saves() => _dispatcher.Invoke(() => _api.Saves.ToList());

    private void Connect()
    {
        _dispatcher.Invoke(() => _controller.Start());
        Assert.True(WaitState(TesterState.Ready), $"Phải đồng bộ xong về Ready, đang {State}.");
    }

    private void ToWaitScan()
    {
        Connect();
        _machine.InsertWire();
        _machine.FinishTest(pass: true);
        Assert.True(WaitState(TesterState.WaitScan), $"Phải nhận RESULT và vào WaitScan, đang {State}.");
    }

    private Task<TesterScanOutcome> ScanAsync(string tag) => _dispatcher.InvokeAsync(() => _controller.HandleScanAsync(tag));

    // S1 — kết nối: HELLO/FW/GM/PINS hiển thị, không cần thao tác nào khác.
    [Fact]
    public void S1_MoCong_HienFwGmPins()
    {
        Connect();

        Assert.Equal("1.0.0", _dispatcher.Invoke(() => _controller.Firmware));
        Assert.Equal("3A7F", _dispatcher.Invoke(() => _controller.GoldenCrc));
        Assert.Equal(16, _dispatcher.Invoke(() => _controller.Pins));
        Assert.Equal(_machine.BootId, _dispatcher.Invoke(() => _controller.BootId));
    }

    // S2 — dây OK -> quét QR -> BIND -> lưu -> SAVED -> UNLOCKED; máy sang WAIT_REMOVE.
    [Fact]
    public async Task S2_DayOk_QuetQr_LuuVaMoKhoa()
    {
        ToWaitScan();

        var outcome = await ScanAsync("TAG1");

        Assert.Equal(TesterScanOutcomeKind.Bound, outcome.Kind);
        Assert.True(WaitState(TesterState.Ready), $"Phải về Ready sau UNLOCKED, đang {State}.");
        var save = Assert.Single(Saves());
        Assert.Equal("TAG1", save.TagCode);
        Assert.Equal(_machine.BootId, save.BootId);
        Assert.Equal(1, save.Seq);
        Assert.Equal("3A7F", save.GoldenCrc);
        Assert.Equal("1.0.0", save.FirmwareVersion);
        Assert.Equal("WAIT_REMOVE", _machine.StateName);
        Assert.Contains(Notices(), n => n.Kind == TesterNoticeKind.Saved && n.Message == "ĐÃ LƯU TAG1");
    }

    // S3 + S3b — dây NG: máy không gửi gì; quét QR -> "DÂY NG", không lưu, có SCAN_ON_NG kèm mã tem.
    [Fact]
    public async Task S3b_DayNgVanQuetQr_BaoDayNgGhiScanOnNgKhongLuu()
    {
        Connect();
        _machine.InsertWire();
        _machine.FinishTest(pass: false);
        Thread.Sleep(80);
        Assert.Equal(TesterState.Ready, State);

        var outcome = await ScanAsync("TAG-NG");

        Assert.Equal(TesterScanOutcomeKind.SentToMachine, outcome.Kind);
        Assert.True(WaitFor(() => Notices().Any(n => n.Kind == TesterNoticeKind.WireNg)));
        Assert.Contains(Events(), e => e.Type == TesterEventType.ScanOnNg && e.TagCode == "TAG-NG");
        Assert.Empty(Saves());
        Assert.Empty(_dispatcher.Invoke(() => _api.Checks.ToList()));

        // S3c: rút dây -> quét lại -> "Chưa thông điện".
        _machine.RemoveWire();
        _notices.Clear();
        await ScanAsync("TAG-NG");
        Assert.True(WaitFor(() => Notices().Any(n => n.Kind == TesterNoticeKind.NotTested)));
    }

    // S3d / S5 — đang thông điện / chưa thông điện.
    [Fact]
    public async Task S3d_QuetKhiMayDangThongDien_BaoDangThongDien()
    {
        Connect();
        _machine.InsertWire();

        await ScanAsync("TAG1");

        Assert.True(WaitFor(() => Notices().Any(n => n.Kind == TesterNoticeKind.Testing)));
        Assert.Empty(Saves());
    }

    [Fact]
    public async Task S5_QuetKhiChuaThongDien_BaoChuaThongDien()
    {
        Connect();

        await ScanAsync("TAG1");

        Assert.True(WaitFor(() => Notices().Any(n => n.Kind == TesterNoticeKind.NotTested)));
    }

    // S4 — có kết quả rồi rút dây: VOID_REMOVED, về Ready, quét QR sau đó bị từ chối ("Chưa thông điện").
    [Fact]
    public async Task S4_RutDayTruocKhiQuet_VoidRemovedVeReady()
    {
        ToWaitScan();

        _machine.RemoveWire();

        Assert.True(WaitState(TesterState.Ready));
        Assert.Contains(Events(), e => e.Type == TesterEventType.VoidRemoved);
        await ScanAsync("TAG1");
        Assert.True(WaitFor(() => Notices().Any(n => n.Kind == TesterNoticeKind.NotTested)));
        Assert.Empty(Saves());
    }

    // S6 — tem bị FR-08 từ chối: không BIND, máy vẫn khóa; quét tem đúng sau đó lưu bình thường.
    [Fact]
    public async Task S6_TemBiTuChoi_MayVanKhoaQuetTemDungSauDoLuu()
    {
        ToWaitScan();
        _api.OnCheck = tag => Task.FromResult(tag == "BAD"
            ? new TagCheckResult(false, "Trùng tem tại công đoạn này.", "dto")
            : new TagCheckResult(true));

        var rejected = await ScanAsync("BAD");

        Assert.Equal(TesterScanOutcomeKind.Rejected, rejected.Kind);
        Assert.Equal("WAIT_BIND", _machine.StateName);
        Assert.Equal(TesterState.WaitScan, State);

        var accepted = await ScanAsync("GOOD");

        Assert.Equal(TesterScanOutcomeKind.Bound, accepted.Kind);
        Assert.True(WaitState(TesterState.Ready));
        Assert.Equal("GOOD", Assert.Single(Saves()).TagCode);
    }

    // S7 — rút cáp USB khi đang chờ quét: chặn quét; cắm lại -> đồng bộ đúng (cùng SEQ -> tiếp tục WaitScan).
    [Fact]
    public async Task S7_RutCapUsbKhiChoQuet_ChanQuetCamLaiDongBoDung()
    {
        ToWaitScan();

        _factory.Unplug();

        Assert.True(WaitState(TesterState.Disconnected));
        var blocked = await ScanAsync("TAG1");
        Assert.Equal(TesterScanOutcomeKind.Blocked, blocked.Kind);
        Assert.Contains(Events(), e => e.Type == TesterEventType.LinkLost);

        _factory.Replug();

        Assert.True(WaitState(TesterState.WaitScan), $"Phải đồng bộ lại về WaitScan, đang {State}.");
        Assert.Equal(1, _dispatcher.Invoke(() => _controller.HeldSeq));
        Assert.Contains(Events(), e => e.Type == TesterEventType.LinkRestored);
        Assert.Empty(Saves());
    }

    // S7b — mất link quá lâu: máy tự hủy kết quả (không gửi được VOID); cắm lại -> VOID_LINK_LOST, Ready.
    [Fact]
    public void S7b_MatLinkQuaLau_MayTuHuy_CamLaiGhiVoidLinkLost()
    {
        ToWaitScan();
        _factory.Unplug();
        Assert.True(WaitState(TesterState.Disconnected));

        Interlocked.Add(ref _simClockMs, 6000);
        _machine.Tick();
        Assert.Equal("WAIT_REMOVE", _machine.StateName);

        _factory.Replug();

        Assert.True(WaitState(TesterState.Ready), $"Phải về Ready, đang {State}.");
        Assert.Contains(Events(), e => e.Type == TesterEventType.VoidLinkLost);
    }

    // S8 — reset máy (EN) khi đang chờ quét: HELLO BOOT mới -> VOID_REBOOT, về Ready.
    [Fact]
    public void S8_ResetMayKhiChoQuet_VoidRebootVeReady()
    {
        ToWaitScan();

        _machine.Boot();

        Assert.True(WaitFor(() => Events().Any(e => e.Type == TesterEventType.VoidReboot)));
        Assert.True(WaitState(TesterState.Ready));
    }

    // S11 — mất server khi đang lưu: không SAVED (máy vẫn khóa, PING vẫn chạy nên không tự hủy); có mạng lại -> Thử lại -> mở khóa.
    [Fact]
    public async Task S11_MatServerKhiLuu_MayVanKhoaThuLaiThanhCongMoKhoa()
    {
        ToWaitScan();
        var failures = 1;
        _api.OnSave = _ => failures-- > 0
            ? throw new InvalidOperationException("Không kết nối được server")
            : Task.FromResult(new TesterSaveResult(true));

        await ScanAsync("TAG1");

        Assert.True(WaitFor(() => _dispatcher.Invoke(() => _controller.CanRetrySave)), "Phải hiện nút Thử lại.");
        Assert.Equal("BINDING", _machine.StateName);

        // Máy vẫn khóa sau nhiều chu kỳ PING (không tự hủy).
        Thread.Sleep(150);
        Assert.Equal("BINDING", _machine.StateName);
        Assert.Contains(Events(), e => e.Type == TesterEventType.DbError);

        _dispatcher.Invoke(() => _controller.RetrySave());

        Assert.True(WaitState(TesterState.Ready), $"Phải về Ready sau Thử lại, đang {State}.");
        Assert.Equal("WAIT_REMOVE", _machine.StateName);
        var saves = Saves();
        Assert.Equal(2, saves.Count);
        Assert.Equal(saves[0], saves[1]);
    }

    // AC11 — Hủy kết quả (Tổ trưởng): DISCARD -> VOID DISCARD, ghi người duyệt.
    [Fact]
    public void Discard_ToTruongHuyKetQua_VeReadyGhiNguoiDuyet()
    {
        ToWaitScan();

        var accepted = _dispatcher.Invoke(() => _controller.Discard("tt01"));

        Assert.True(accepted);
        Assert.True(WaitState(TesterState.Ready));
        Assert.Contains(Events(), e => e.Type == TesterEventType.VoidDiscard && e.ApprovedBy == "tt01");
        Assert.Equal("WAIT_REMOVE", _machine.StateName);
    }

    // US-14 AC3/AC4 — máy ngừng trả PONG: mất kết nối, chặn quét; trả lời lại -> tự kết nối lại.
    [Fact]
    public void MayNgungTraPong_MatKetNoiRoiTuKetNoiLai()
    {
        Connect();

        _machine.RespondToPing = false;
        Assert.True(WaitState(TesterState.Disconnected), "Phải mất kết nối khi không có PONG.");

        _machine.RespondToPing = true;
        Assert.True(WaitState(TesterState.Ready), "Phải tự kết nối lại và đồng bộ.");
    }

    // US-14 AC8 — máy chưa có mẫu.
    [Fact]
    public void MayChuaCoMau_BaoNoSampleNhungVanKetNoi()
    {
        _machine.GoldenCrc = "0000";

        Connect();

        Assert.True(_dispatcher.Invoke(() => _controller.NoSample));
        Assert.Equal(TesterState.Ready, State);
    }

    // US-11 AC7/AC8 — tắt công tắc: đóng cổng, máy mất link; bật lại: mở cổng, đồng bộ.
    [Fact]
    public void TatRoiBatLaiCongTac_DongMoCong()
    {
        Connect();

        _dispatcher.Invoke(() => _controller.Stop());
        Assert.Equal(TesterState.Stopped, State);
        Assert.False(_link.IsPortOpen);

        _dispatcher.Invoke(() => _controller.Start());
        Assert.True(WaitState(TesterState.Ready));
        Assert.True(_link.IsPortOpen);
    }
}
