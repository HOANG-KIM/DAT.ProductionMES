using ProductionMES.Station.Tester.Protocol;
using ProductionMES.Station.Tester.Station;
using ProductionMES.Station.Tester.Tests.Fakes;

namespace ProductionMES.Station.Tester.Tests;

/// <summary>
/// State machine App (US-12, US-14 AC5-AC8) với link/server/hẹn giờ giả — bám bảng spec-app 7.1 / 7.3 / 7.5 và các AC.
/// </summary>
public class TesterStationControllerTests
{
    private sealed class Harness
    {
        public FakeTesterLink Link { get; } = new();

        public FakeServerApi Api { get; } = new();

        public ManualScheduler Scheduler { get; } = new();

        public List<TesterNotice> Notices { get; } = new();

        public List<(string Tag, object? Payload)> SavedEvents { get; } = new();

        public TesterStationController Sut { get; }

        public Harness()
        {
            Sut = new TesterStationController(
                Link, Api, new ImmediateDispatcher(), Scheduler,
                new TesterOptions { CommandTimeoutMs = 2000 }, NullTesterLog.Instance,
                () => new DateTime(2026, 10, 12, 8, 30, 0));
            Sut.NoticeRaised += Notices.Add;
            Sut.ScanSaved += (tag, payload) => SavedEvents.Add((tag, payload));
        }

        /// <summary>Mở link, máy gửi HELLO + PONG đầu tiên (không giữ kết quả) -> Ready.</summary>
        public void Connect(string boot = "8F21C04D", string gm = "3A7F", string pong = "#PONG,ST=IDLE,SEQ=0")
        {
            Sut.Start();
            Link.OpenPort();
            Link.Receive($"#HELLO,FW=1.0.0,PINS=16,GM={gm},BOOT={boot}");
            Link.Receive(pong);
        }

        public void TestOk(int seq = 105, string gm = "3A7F") => Link.Receive($"#RESULT,SEQ={seq},R=OK,GM={gm}");

        public void ToWaitScan(int seq = 105)
        {
            Connect();
            TestOk(seq);
        }

        /// <summary>Quét tem hợp lệ ở WaitScan -> Binding.</summary>
        public async Task ToBindingAsync(string tag = "TAG1", int seq = 105)
        {
            ToWaitScan(seq);
            var outcome = await Sut.HandleScanAsync(tag);
            Assert.Equal(TesterScanOutcomeKind.Bound, outcome.Kind);
        }

        /// <summary>Tới Saving (đã BIND_OK, lưu server xong hoặc đang chờ tùy OnSave).</summary>
        public async Task ToSavingAsync(string tag = "TAG1", int seq = 105)
        {
            await ToBindingAsync(tag, seq);
            Link.Receive($"#BIND_OK,SEQ={seq}");
        }

        public TesterNotice? LastNotice => Notices.Count == 0 ? null : Notices[^1];
    }

    // ------------------------------------------------------------------------------------------------------
    // Kết nối & đồng bộ ban đầu (US-14)
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Start_MoLinkVaChanQuetChoToiKhiDongBo()
    {
        var h = new Harness();

        h.Sut.Start();

        Assert.True(h.Link.Started);
        Assert.Equal(TesterState.Disconnected, h.Sut.State);
        Assert.False(h.Sut.IsConnected);
        var outcome = await h.Sut.HandleScanAsync("TAG1");
        Assert.Equal(TesterScanOutcomeKind.Blocked, outcome.Kind);
        Assert.Empty(h.Link.Sent);
        Assert.Empty(h.Api.Checks);
    }

    [Fact]
    public void DongBo_HelloVaPongIdle_VeReadyVaHienThongTinMay()
    {
        var h = new Harness();

        h.Connect();

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.True(h.Sut.IsConnected);
        Assert.Equal("1.0.0", h.Sut.Firmware);
        Assert.Equal(16, h.Sut.Pins);
        Assert.Equal("3A7F", h.Sut.GoldenCrc);
        Assert.Equal("8F21C04D", h.Sut.BootId);
        Assert.Equal(TesterTexts.Ready, h.Sut.StatusText);
        Assert.False(h.Sut.NoSample);
    }

    [Fact]
    public void Disconnected_ChuPongDauTien_VanChanQuet()
    {
        var h = new Harness();
        h.Sut.Start();
        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");

        Assert.Equal(TesterState.Disconnected, h.Sut.State);
        Assert.Equal(TesterTexts.Disconnected, h.Sut.StatusText);
    }

    // ------------------------------------------------------------------------------------------------------
    // US-12 AC1 — RESULT -> WaitScan
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public void Ready_NhanResultOk_VeWaitScanVaGiuSeqKhongLuuGi()
    {
        var h = new Harness();
        h.Connect();

        h.TestOk(105);

        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(105, h.Sut.HeldSeq);
        Assert.Equal(TesterTexts.WaitScan, h.Sut.StatusText);
        Assert.Empty(h.Api.Saves);
        Assert.DoesNotContain(h.Link.Sent, c => c.Kind == TesterCommandKind.Bind);
    }

    // ------------------------------------------------------------------------------------------------------
    // US-12 AC2 — happy path, AC4 — kiểm tra trước, lưu sau, SAVED chỉ sau commit
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task HappyPath_KiemTraTemRoiBindRoiLuuRoiSavedRoiUnlocked()
    {
        var h = new Harness();
        h.ToWaitScan(105);

        var outcome = await h.Sut.HandleScanAsync("TAG1");

        Assert.Equal(TesterScanOutcomeKind.Bound, outcome.Kind);
        Assert.Equal(new[] { "TAG1" }, h.Api.Checks);
        Assert.Equal(TesterCommand.Bind(105), h.Link.Sent[^1]);
        Assert.Equal(TesterState.Binding, h.Sut.State);
        Assert.Equal(TesterTexts.Binding, h.Sut.StatusText);
        Assert.Empty(h.Api.Saves);

        h.Link.Receive("#BIND_OK,SEQ=105");

        var save = Assert.Single(h.Api.Saves);
        Assert.Equal("TAG1", save.TagCode);
        Assert.Equal("8F21C04D", save.BootId);
        Assert.Equal(105, save.Seq);
        Assert.Equal("3A7F", save.GoldenCrc);
        Assert.Equal("1.0.0", save.FirmwareVersion);
        Assert.Equal(new DateTime(2026, 10, 12, 8, 30, 0), save.TestedAt);
        Assert.Equal(TesterCommand.Saved(105), h.Link.Sent[^1]);
        Assert.Equal(TesterState.Saving, h.Sut.State);
        var savedEvent = Assert.Single(h.SavedEvents);
        Assert.Equal("TAG1", savedEvent.Tag);
        Assert.Equal("save-payload", savedEvent.Payload);

        h.Link.Receive("#UNLOCKED,SEQ=105");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Null(h.Sut.HeldSeq);
        Assert.Equal(TesterNoticeKind.Saved, h.LastNotice!.Kind);
        Assert.Equal("ĐÃ LƯU TAG1", h.LastNotice.Message);
    }

    [Fact]
    public async Task Saved_ChiDuocGuiSauKhiServerCommit()
    {
        var h = new Harness();
        var tcs = new TaskCompletionSource<TesterSaveResult>();
        h.Api.OnSave = _ => tcs.Task;
        await h.ToBindingAsync();

        h.Link.Receive("#BIND_OK,SEQ=105");

        Assert.Single(h.Api.Saves);
        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.Equal(TesterState.Saving, h.Sut.State);

        tcs.SetResult(new TesterSaveResult(true));

        Assert.Equal(1, h.Link.CountSent(TesterCommandKind.Saved));
    }

    // ------------------------------------------------------------------------------------------------------
    // US-12 AC3 — tem bị từ chối: KHÔNG BIND, máy vẫn khóa
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task TemBiTuChoi_KhongBindOWaitScanVaCoPayloadChoBanner()
    {
        var h = new Harness();
        h.Api.OnCheck = _ => Task.FromResult(new TagCheckResult(false, "Trùng tem tại công đoạn này.", "dto"));
        h.ToWaitScan();

        var outcome = await h.Sut.HandleScanAsync("TAG1");

        Assert.Equal(TesterScanOutcomeKind.Rejected, outcome.Kind);
        Assert.Equal("Trùng tem tại công đoạn này.", outcome.Message);
        Assert.Equal("dto", outcome.RejectionPayload);
        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Bind));
        Assert.Empty(h.Api.Saves);

        // Quét tem đúng sau đó vẫn lưu bình thường (nghiệm thu S6).
        h.Api.OnCheck = _ => Task.FromResult(new TagCheckResult(true));
        var second = await h.Sut.HandleScanAsync("TAG2");
        Assert.Equal(TesterScanOutcomeKind.Bound, second.Kind);
        Assert.Equal(TesterCommand.Bind(105), h.Link.Sent[^1]);
    }

    [Fact]
    public async Task KiemTraTemLoiServer_ServerErrorVaVanOWaitScan()
    {
        var h = new Harness();
        h.Api.OnCheck = _ => throw new InvalidOperationException("Mất kết nối server");
        h.ToWaitScan();

        var outcome = await h.Sut.HandleScanAsync("TAG1");

        Assert.Equal(TesterScanOutcomeKind.ServerError, outcome.Kind);
        Assert.Equal("Mất kết nối server", outcome.Message);
        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Bind));
    }

    [Fact]
    public async Task RutDayTrongLucKiemTraTem_KhongBind()
    {
        var h = new Harness();
        var tcs = new TaskCompletionSource<TagCheckResult>();
        h.Api.OnCheck = _ => tcs.Task;
        h.ToWaitScan();

        var scanTask = h.Sut.HandleScanAsync("TAG1");
        h.Link.Receive("#VOID,SEQ=105,REASON=REMOVED");
        tcs.SetResult(new TagCheckResult(true));
        var outcome = await scanTask;

        Assert.Equal(TesterScanOutcomeKind.Aborted, outcome.Kind);
        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Bind));
    }

    [Fact]
    public async Task QuetLanHaiTrongLucDangKiemTra_BiBoVoiBusy()
    {
        var h = new Harness();
        var tcs = new TaskCompletionSource<TagCheckResult>();
        h.Api.OnCheck = _ => tcs.Task;
        h.ToWaitScan();

        var first = h.Sut.HandleScanAsync("TAG1");
        var second = await h.Sut.HandleScanAsync("TAG2");
        tcs.SetResult(new TagCheckResult(true));
        await first;

        Assert.Equal(TesterScanOutcomeKind.Busy, second.Kind);
        Assert.Single(h.Api.Checks);
    }

    // ------------------------------------------------------------------------------------------------------
    // US-12 AC5/AC6 — rút dây
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public void RutDayKhiWaitScan_GhiVoidRemovedVeReadyKhongLuu()
    {
        var h = new Harness();
        h.ToWaitScan(105);

        h.Link.Receive("#VOID,SEQ=105,REASON=REMOVED");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Null(h.Sut.HeldSeq);
        var ev = Assert.Single(h.Api.Events);
        Assert.Equal(TesterEventType.VoidRemoved, ev.Type);
        Assert.Equal(105, ev.Seq);
        Assert.Equal(TesterNoticeKind.WireRemoved, h.LastNotice!.Kind);
        Assert.Equal("Dây đã bị rút — thông điện lại", h.LastNotice.Message);
        Assert.Empty(h.Api.Saves);
    }

    [Fact]
    public async Task VoidRemovedKhiBinding_KhongLuuVeReady()
    {
        var h = new Harness();
        await h.ToBindingAsync();

        h.Link.Receive("#VOID,SEQ=105,REASON=REMOVED");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Empty(h.Api.Saves);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.VoidRemoved);
        Assert.Equal(0, h.Scheduler.ActiveCount);
    }

    [Theory]
    [InlineData("NO_PENDING")]
    [InlineData("REMOVED")]
    public async Task BindFailNoPendingKhiBinding_DayDaRutKhongLuuVeReady(string reason)
    {
        var h = new Harness();
        await h.ToBindingAsync();

        h.Link.Receive($"#BIND_FAIL,SEQ=105,REASON={reason}");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Empty(h.Api.Saves);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.VoidRemoved);
        Assert.Equal(TesterNoticeKind.WireRemoved, h.LastNotice!.Kind);
    }

    // ------------------------------------------------------------------------------------------------------
    // US-12 AC7/AC8/AC9 — quét QR khi không giữ kết quả (Ready)
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task QuetOReady_GuiBindSeq0NgayKhongGoiServerKiemTra()
    {
        var h = new Harness();
        h.Connect();

        var outcome = await h.Sut.HandleScanAsync("TAG1");

        Assert.Equal(TesterScanOutcomeKind.SentToMachine, outcome.Kind);
        Assert.Equal("#BIND,SEQ=0", h.Link.Sent[^1].ToLine());
        Assert.Empty(h.Api.Checks);
        Assert.Empty(h.Api.Saves);
        Assert.Equal(TesterState.Ready, h.Sut.State);
    }

    [Fact]
    public async Task QuetOReady_DayNg_ThongBaoDoGhiScanOnNgKemMaTem()
    {
        var h = new Harness();
        h.Connect();
        await h.Sut.HandleScanAsync("TAG-NG");

        h.Link.Receive("#BIND_FAIL,SEQ=0,REASON=NG");

        Assert.Equal(TesterNoticeKind.WireNg, h.LastNotice!.Kind);
        Assert.Equal("DÂY NG — dùng chế độ Scan NG", h.LastNotice.Message);
        var ev = Assert.Single(h.Api.Events);
        Assert.Equal(TesterEventType.ScanOnNg, ev.Type);
        Assert.Equal("TAG-NG", ev.TagCode);
        Assert.Empty(h.Api.Saves);
        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Equal(0, h.Scheduler.ActiveCount);
    }

    [Fact]
    public async Task QuetOReady_ChuaThongDien_ThongBaoChuaThongDien()
    {
        var h = new Harness();
        h.Connect();
        await h.Sut.HandleScanAsync("TAG1");

        h.Link.Receive("#BIND_FAIL,SEQ=0,REASON=NO_PENDING");

        Assert.Equal("Chưa thông điện", h.LastNotice!.Message);
        Assert.Empty(h.Api.Events);
        Assert.Empty(h.Api.Saves);
    }

    [Fact]
    public async Task QuetOReady_DangThongDien_ThongBaoChoKetQuaKhongGhiEvent()
    {
        var h = new Harness();
        h.Connect();
        await h.Sut.HandleScanAsync("TAG1");

        h.Link.Receive("#BIND_FAIL,SEQ=0,REASON=TESTING");

        Assert.Equal("Đang thông điện — chờ kết quả rồi quét lại", h.LastNotice!.Message);
        Assert.Empty(h.Api.Events);
    }

    [Fact]
    public async Task QuetOReady_BadSeq_GhiBindFailGuiResultQueryNhanKetQuaVeWaitScan()
    {
        var h = new Harness();
        h.Connect();
        await h.Sut.HandleScanAsync("TAG1");

        h.Link.Receive("#BIND_FAIL,SEQ=0,REASON=BAD_SEQ");

        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.BindFail);
        Assert.Equal(TesterCommand.ResultQuery(), h.Link.Sent[^1]);

        h.TestOk(107);

        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(107, h.Sut.HeldSeq);
        Assert.Equal(TesterNoticeKind.ResultRecovered, h.LastNotice!.Kind);
    }

    [Fact]
    public async Task QuetOReady_ChoPhanHoiNhungDangBan_QuetTiepBiBo()
    {
        var h = new Harness();
        h.Connect();
        await h.Sut.HandleScanAsync("TAG1");

        var second = await h.Sut.HandleScanAsync("TAG2");

        Assert.Equal(TesterScanOutcomeKind.Busy, second.Kind);
        Assert.Equal(1, h.Link.CountSent(TesterCommandKind.Bind));
    }

    // ------------------------------------------------------------------------------------------------------
    // US-12 AC10 — BUSY và không phản hồi
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task BusyKhiBinding_KhongGuiTrungDoiPongBindingRoiTiepTucSaving()
    {
        var h = new Harness();
        await h.ToBindingAsync();
        var bindsBefore = h.Link.CountSent(TesterCommandKind.Bind);

        h.Link.Receive("#BIND_FAIL,SEQ=105,REASON=BUSY");

        Assert.Equal(TesterNoticeKind.BindBusy, h.LastNotice!.Kind);
        Assert.Equal("Đang xác nhận lần trước — vui lòng chờ", h.LastNotice.Message);
        Assert.Equal(bindsBefore, h.Link.CountSent(TesterCommandKind.Bind));
        Assert.Equal(TesterState.Binding, h.Sut.State);
        Assert.Empty(h.Api.Saves);

        h.Link.Receive("#PONG,ST=BINDING,SEQ=105");

        Assert.Equal(TesterState.Saving, h.Sut.State);
        Assert.Single(h.Api.Saves);
        Assert.Equal(TesterCommand.Saved(105), h.Link.Sent[^1]);
    }

    [Fact]
    public async Task ErrBusyKhiBinding_GiongBindFailBusy()
    {
        var h = new Harness();
        await h.ToBindingAsync();
        var bindsBefore = h.Link.CountSent(TesterCommandKind.Bind);

        h.Link.Receive("#ERR,CODE=BUSY");

        Assert.Equal(TesterNoticeKind.BindBusy, h.LastNotice!.Kind);
        Assert.Equal(bindsBefore, h.Link.CountSent(TesterCommandKind.Bind));
    }

    [Fact]
    public async Task BindKhongPhanHoi_GuiLaiToiDa2LanRoiVeWaitScanBaoLoiGhiBindFail()
    {
        var h = new Harness();
        await h.ToBindingAsync();
        Assert.Equal(1, h.Link.CountSent(TesterCommandKind.Bind));

        h.Scheduler.FireAll();
        Assert.Equal(2, h.Link.CountSent(TesterCommandKind.Bind));
        h.Scheduler.FireAll();
        Assert.Equal(3, h.Link.CountSent(TesterCommandKind.Bind));
        Assert.Equal(TesterState.Binding, h.Sut.State);

        h.Scheduler.FireAll();

        Assert.Equal(3, h.Link.CountSent(TesterCommandKind.Bind));
        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(TesterNoticeKind.NoResponse, h.LastNotice!.Kind);
        Assert.Equal("Không phản hồi từ máy thông điện", h.LastNotice.Message);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.BindFail);
        Assert.Empty(h.Api.Saves);
    }

    [Fact]
    public async Task BindOMuonSauKhiHetLuotGuiLai_VanTiepTucSaving()
    {
        var h = new Harness();
        await h.ToBindingAsync();
        h.Scheduler.FireAll();
        h.Scheduler.FireAll();
        h.Scheduler.FireAll();
        Assert.Equal(TesterState.WaitScan, h.Sut.State);

        h.Link.Receive("#BIND_OK,SEQ=105");

        Assert.Equal(TesterState.Saving, h.Sut.State);
        Assert.Single(h.Api.Saves);
    }

    [Fact]
    public async Task BindOReadyKhongPhanHoi_BaoLoiSauLanGuiThu3()
    {
        var h = new Harness();
        h.Connect();
        await h.Sut.HandleScanAsync("TAG1");

        h.Scheduler.FireAll();
        h.Scheduler.FireAll();
        h.Scheduler.FireAll();

        Assert.Equal(3, h.Link.CountSent(TesterCommandKind.Bind));
        Assert.Equal(TesterNoticeKind.NoResponse, h.LastNotice!.Kind);
        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.BindFail);
    }

    [Fact]
    public async Task BusyOReady_ThuLai2LanRoiBaoBan()
    {
        var h = new Harness();
        h.Connect();
        await h.Sut.HandleScanAsync("TAG1");

        h.Link.Receive("#BIND_FAIL,SEQ=0,REASON=BUSY");
        h.Link.Receive("#BIND_FAIL,SEQ=0,REASON=BUSY");
        Assert.Equal(3, h.Link.CountSent(TesterCommandKind.Bind));
        h.Link.Receive("#BIND_FAIL,SEQ=0,REASON=BUSY");

        Assert.Equal(3, h.Link.CountSent(TesterCommandKind.Bind));
        Assert.Equal(TesterNoticeKind.BindBusy, h.LastNotice!.Kind);
    }

    // ------------------------------------------------------------------------------------------------------
    // US-12 AC11 — Hủy kết quả (DISCARD)
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public void Discard_OWaitScan_GuiDiscardRoiVoidDiscardVeReadyGhiNguoiDuyet()
    {
        var h = new Harness();
        h.ToWaitScan(105);
        Assert.True(h.Sut.CanDiscard);

        var accepted = h.Sut.Discard("tt01");

        Assert.True(accepted);
        Assert.Equal(TesterCommand.Discard(105), h.Link.Sent[^1]);
        Assert.Equal(TesterState.Discarding, h.Sut.State);

        h.Link.Receive("#VOID,SEQ=105,REASON=DISCARD");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        var ev = Assert.Single(h.Api.Events);
        Assert.Equal(TesterEventType.VoidDiscard, ev.Type);
        Assert.Equal("tt01", ev.ApprovedBy);
        Assert.Equal(105, ev.Seq);
        Assert.Empty(h.Api.Saves);
    }

    [Fact]
    public async Task Discard_KhongChoPhepODangBindingHoacReady()
    {
        var h = new Harness();
        h.Connect();
        Assert.False(h.Sut.Discard("tt01"));

        h.TestOk(105);
        await h.Sut.HandleScanAsync("TAG1");
        Assert.Equal(TesterState.Binding, h.Sut.State);
        Assert.False(h.Sut.Discard("tt01"));
        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Discard));
    }

    [Fact]
    public void Discard_KhongPhanHoi_VeLaiWaitScanBaoLoi()
    {
        var h = new Harness();
        h.ToWaitScan(105);
        h.Sut.Discard("tt01");

        h.Scheduler.FireAll();
        h.Scheduler.FireAll();
        h.Scheduler.FireAll();

        Assert.Equal(3, h.Link.CountSent(TesterCommandKind.Discard));
        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(TesterNoticeKind.NoResponse, h.LastNotice!.Kind);
    }

    // ------------------------------------------------------------------------------------------------------
    // US-12 AC13/AC14/AC15 — lỗi lưu server, Thử lại, không hàng đợi offline
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task LuuLoi_KhongGuiSavedGhiDbErrorChoThuLaiVaKhiThanhCongMoKhoa()
    {
        var h = new Harness();
        var calls = 0;
        h.Api.OnSave = _ =>
        {
            calls++;
            if (calls == 1)
            {
                throw new InvalidOperationException("Không kết nối được server");
            }

            return Task.FromResult(new TesterSaveResult(true));
        };
        await h.ToSavingAsync();

        Assert.Equal(TesterState.Saving, h.Sut.State);
        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.Equal(TesterTexts.SaveFailed, h.Sut.StatusText);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.DbError);
        Assert.Equal(TesterNoticeKind.SaveFailed, h.LastNotice!.Kind);
        Assert.True(h.Sut.CanRetrySave);
        Assert.True(h.Sut.CanDiscard);

        var retried = h.Sut.RetrySave();

        Assert.True(retried);
        Assert.Equal(2, h.Api.Saves.Count);
        // Cùng bộ khóa (BOOT, SEQ) ở cả 2 lần gọi -> server idempotent (AC15).
        Assert.Equal(h.Api.Saves[0], h.Api.Saves[1]);
        Assert.Equal(TesterCommand.Saved(105), h.Link.Sent[^1]);

        h.Link.Receive("#UNLOCKED,SEQ=105");
        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Equal(TesterNoticeKind.Saved, h.LastNotice!.Kind);
    }

    [Fact]
    public async Task LuuBiServerTuChoi_CungXuLyNhuLoi()
    {
        var h = new Harness();
        h.Api.OnSave = _ => Task.FromResult(new TesterSaveResult(false, "Trùng tem tại công đoạn này."));
        await h.ToSavingAsync();

        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.True(h.Sut.CanRetrySave);
        Assert.Contains("Trùng tem", h.LastNotice!.Message);
    }

    [Fact]
    public async Task LuuLoi_ToTruongHuyKetQua_GuiDiscardKhongSaved()
    {
        var h = new Harness();
        h.Api.OnSave = _ => throw new InvalidOperationException("mất mạng");
        await h.ToSavingAsync();

        Assert.True(h.Sut.Discard("tt01"));
        Assert.Equal(TesterCommand.Discard(105), h.Link.Sent[^1]);

        h.Link.Receive("#VOID,SEQ=105,REASON=DISCARD");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.VoidDiscard && e.ApprovedBy == "tt01");
    }

    [Fact]
    public async Task DangLuuDoKhongChoHuy()
    {
        var h = new Harness();
        var tcs = new TaskCompletionSource<TesterSaveResult>();
        h.Api.OnSave = _ => tcs.Task;
        await h.ToSavingAsync();

        Assert.False(h.Sut.CanDiscard);
        Assert.False(h.Sut.Discard("tt01"));
        Assert.False(h.Sut.CanRetrySave);
    }

    [Fact]
    public async Task SavedKhongNhanUnlocked_ChoThuLaiGuiLaiSavedKhongLuuLaiServer()
    {
        var h = new Harness();
        await h.ToSavingAsync();
        Assert.Equal(1, h.Link.CountSent(TesterCommandKind.Saved));

        h.Scheduler.FireAll();
        h.Scheduler.FireAll();
        h.Scheduler.FireAll();

        Assert.Equal(3, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.Equal(TesterState.Saving, h.Sut.State);
        Assert.Equal(TesterTexts.NotUnlocked, h.Sut.StatusText);
        Assert.True(h.Sut.CanRetrySave);
        Assert.False(h.Sut.CanDiscard);

        h.Sut.RetrySave();

        Assert.Equal(4, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.Single(h.Api.Saves);
    }

    [Fact]
    public async Task DayBiRutTrongLucLuu_BanGhiVanHopLeKhongGuiSaved()
    {
        var h = new Harness();
        var tcs = new TaskCompletionSource<TesterSaveResult>();
        h.Api.OnSave = _ => tcs.Task;
        await h.ToSavingAsync();

        h.Link.Receive("#VOID,SEQ=105,REASON=REMOVED");
        tcs.SetResult(new TesterSaveResult(true));

        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Single(h.SavedEvents);
    }

    // ------------------------------------------------------------------------------------------------------
    // US-14 AC3/AC5/AC6 — mất kết nối & đồng bộ lại
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task MatKetNoiKhiWaitScan_ChanQuetGhiLinkLost()
    {
        var h = new Harness();
        h.ToWaitScan();

        h.Link.ClosePort("Không nhận PONG");

        Assert.Equal(TesterState.Disconnected, h.Sut.State);
        Assert.Equal(TesterTexts.Disconnected, h.Sut.StatusText);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.LinkLost);
        var outcome = await h.Sut.HandleScanAsync("TAG1");
        Assert.Equal(TesterScanOutcomeKind.Blocked, outcome.Kind);
        Assert.Empty(h.Api.Checks);
        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Bind));
        Assert.False(h.Sut.CanDiscard);
    }

    [Fact]
    public void KetNoiLai_PongWaitBindCungSeq_TiepTucWaitScanGhiLinkRestored()
    {
        var h = new Harness();
        h.ToWaitScan(105);
        h.Link.ClosePort();

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=WAIT_BIND,SEQ=105");

        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(105, h.Sut.HeldSeq);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.LinkRestored);
    }

    [Fact]
    public void KetNoiLai_BootKhac_VoidRebootVeReady()
    {
        var h = new Harness();
        h.ToWaitScan(105);
        h.Link.ClosePort();

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=11112222");
        h.Link.Receive("#PONG,ST=IDLE,SEQ=0");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Null(h.Sut.HeldSeq);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.VoidReboot);
        Assert.DoesNotContain(h.Api.Events, e => e.Type == TesterEventType.VoidLinkLost);
    }

    [Fact]
    public void MayResetKhiDangCho_HelloBootMoi_VoidRebootVeReadyKhongCanMatLink()
    {
        var h = new Harness();
        h.ToWaitScan(105);

        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=11112222");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Null(h.Sut.HeldSeq);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.VoidReboot);
        Assert.Equal(TesterNoticeKind.MachineReset, h.LastNotice!.Kind);
    }

    [Fact]
    public void KetNoiLai_PongSeq0KhiAppDangGiu_VoidLinkLostVeReady()
    {
        var h = new Harness();
        h.ToWaitScan(105);
        h.Link.ClosePort();

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=IDLE,SEQ=0");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.VoidLinkLost);
        Assert.Equal(TesterNoticeKind.ResultVoidedByMachine, h.LastNotice!.Kind);
    }

    [Fact]
    public void KetNoiLai_PongSeq0KhongGiuGi_VeReady()
    {
        var h = new Harness();
        h.Connect();
        h.Link.ClosePort();

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=IDLE,SEQ=0");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.DoesNotContain(h.Api.Events, e => e.Type == TesterEventType.VoidLinkLost);
    }

    [Fact]
    public void KetNoiLai_MayGiuKetQuaAppKhongGiu_GuiResultQueryRoiWaitScan()
    {
        var h = new Harness();
        h.Connect();
        h.Link.ClosePort();

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=WAIT_BIND,SEQ=107");

        Assert.Equal(TesterCommand.ResultQuery(), h.Link.Sent[^1]);
        Assert.NotEqual(TesterState.WaitScan, h.Sut.State);

        h.TestOk(107);

        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(107, h.Sut.HeldSeq);
    }

    [Fact]
    public void KetNoiLai_MayGiuSeqKhacAppGiu_ThayBangKetQuaMayGiu()
    {
        var h = new Harness();
        h.ToWaitScan(105);
        h.Link.ClosePort();

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=WAIT_BIND,SEQ=106");
        h.TestOk(106);

        Assert.Equal(106, h.Sut.HeldSeq);
        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Contains(h.Api.Events, e => e.Type == TesterEventType.VoidLinkLost && e.Seq == 105);
    }

    [Fact]
    public async Task MatKetNoiSauKhiDaCommit_KetNoiLaiPongBinding_GuiLaiSavedKhongLuuLai()
    {
        var h = new Harness();
        await h.ToSavingAsync();
        Assert.Equal(1, h.Link.CountSent(TesterCommandKind.Saved));
        h.Link.ClosePort();
        Assert.Equal(TesterState.Disconnected, h.Sut.State);

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=BINDING,SEQ=105");

        Assert.Equal(TesterState.Saving, h.Sut.State);
        Assert.Equal(2, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.Single(h.Api.Saves);

        h.Link.Receive("#UNLOCKED,SEQ=105");
        Assert.Equal(TesterState.Ready, h.Sut.State);
    }

    // AC16: máy tự mở khóa sau 5 giây mất link — bản ghi đã commit vẫn hợp lệ, không coi là sự cố.
    [Fact]
    public async Task MatKetNoiSauKhiDaCommit_MayTuMoKhoa_KetNoiLaiPongSeq0_KhongGhiVoid()
    {
        var h = new Harness();
        await h.ToSavingAsync();
        h.Link.ClosePort();

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=WAIT_REMOVE,SEQ=0");

        Assert.Equal(TesterState.Ready, h.Sut.State);
        Assert.DoesNotContain(h.Api.Events, e => e.Type is TesterEventType.VoidLinkLost or TesterEventType.VoidReboot);
        Assert.Equal(TesterNoticeKind.Saved, h.LastNotice!.Kind);
    }

    [Fact]
    public async Task MatKetNoiTrongLucDangLuu_LuuXongKhongGuiSavedChoDenKhiDongBoLai()
    {
        var h = new Harness();
        var tcs = new TaskCompletionSource<TesterSaveResult>();
        h.Api.OnSave = _ => tcs.Task;
        await h.ToSavingAsync();
        h.Link.ClosePort();

        tcs.SetResult(new TesterSaveResult(true));

        Assert.Equal(0, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.Equal(TesterState.Disconnected, h.Sut.State);

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=BINDING,SEQ=105");

        Assert.Equal(1, h.Link.CountSent(TesterCommandKind.Saved));
        Assert.Single(h.Api.Saves);
    }

    [Fact]
    public void ResultKhiChuaDongBo_BiBoQuaVaDongBoSeLayLaiBangResultQuery()
    {
        var h = new Harness();
        h.Sut.Start();
        h.Link.OpenPort();
        h.TestOk(105);
        Assert.Null(h.Sut.HeldSeq);

        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=WAIT_BIND,SEQ=105");
        h.TestOk(105);

        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(105, h.Sut.HeldSeq);
    }

    [Fact]
    public void VoidKhiChuaDongBo_KhongMoKhoaQuetTruocPong()
    {
        var h = new Harness();
        h.ToWaitScan(105);
        h.Link.ClosePort();

        h.Link.Receive("#VOID,SEQ=105,REASON=REMOVED");

        Assert.Equal(TesterState.Disconnected, h.Sut.State);
    }

    // ------------------------------------------------------------------------------------------------------
    // US-14 AC7/AC8 — mọi ST và ERR, banner thiếu mẫu
    // ------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("IDLE")]
    [InlineData("TESTING")]
    [InlineData("WAIT_REMOVE")]
    [InlineData("BROWSE")]
    [InlineData("NO_SAMPLE")]
    [InlineData("SOMETHING_NEW")]
    public void PongCacTrangThaiKhac_KhongDoiTrangThaiApp(string st)
    {
        var h = new Harness();
        h.ToWaitScan(105);

        h.Link.Receive($"#PONG,ST={st},SEQ=0");

        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(105, h.Sut.HeldSeq);
    }

    [Theory]
    [InlineData("BAD_CMD")]
    [InlineData("BAD_SEQ")]
    [InlineData("NO_PENDING")]
    [InlineData("TOO_LONG")]
    [InlineData("BUSY")]
    [InlineData("WHATEVER")]
    public void ErrKhiKhongChoLenh_GhiLogKhongDoiTrangThai(string code)
    {
        var h = new Harness();
        h.ToWaitScan(105);

        h.Link.Receive($"#ERR,CODE={code}");

        Assert.Equal(TesterState.WaitScan, h.Sut.State);
        Assert.Equal(105, h.Sut.HeldSeq);
    }

    [Fact]
    public async Task ErrBadCmdKhiQuetOReady_BaoLoiMayVaKhongTreo()
    {
        var h = new Harness();
        h.Connect();
        await h.Sut.HandleScanAsync("TAG1");

        h.Link.Receive("#ERR,CODE=BAD_CMD");

        Assert.Equal(TesterNoticeKind.MachineError, h.LastNotice!.Kind);
        Assert.Equal(0, h.Scheduler.ActiveCount);
        var next = await h.Sut.HandleScanAsync("TAG2");
        Assert.Equal(TesterScanOutcomeKind.SentToMachine, next.Kind);
    }

    [Fact]
    public void HelloGm0000_BaoChuaCoMau()
    {
        var h = new Harness();

        h.Connect(gm: "0000");

        Assert.True(h.Sut.NoSample);
        Assert.Equal(TesterState.Ready, h.Sut.State);
    }

    [Fact]
    public void PongNoSample_BatCoChuaCoMau_KhongChanCong()
    {
        var h = new Harness();
        h.Connect();

        h.Link.Receive("#PONG,ST=NO_SAMPLE,SEQ=0");

        Assert.True(h.Sut.NoSample);
        Assert.True(h.Sut.IsConnected);
    }

    // ------------------------------------------------------------------------------------------------------
    // US-11 AC7/AC8/AC9 — dừng/bật lại, đổi công tắc
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Stop_DongLinkChanQuetStartLaiMoLai()
    {
        var h = new Harness();
        h.ToWaitScan();

        h.Sut.Stop();

        Assert.Equal(TesterState.Stopped, h.Sut.State);
        Assert.Equal(1, h.Link.StopCount);
        Assert.Null(h.Sut.HeldSeq);
        var outcome = await h.Sut.HandleScanAsync("TAG1");
        Assert.Equal(TesterScanOutcomeKind.Blocked, outcome.Kind);

        h.Sut.Start();

        Assert.Equal(TesterState.Disconnected, h.Sut.State);
        Assert.Equal(2, h.Link.StartCount);
    }

    [Fact]
    public void Stop_BoQuaMoiSuKienTuLink()
    {
        var h = new Harness();
        h.Connect();
        h.Sut.Stop();

        h.Link.Receive("#RESULT,SEQ=1,R=OK,GM=3A7F");
        h.Link.ClosePort();

        Assert.Equal(TesterState.Stopped, h.Sut.State);
        Assert.Empty(h.Api.Events);
    }

    [Fact]
    public async Task CanChangeMode_ChiKhiKhongCoKetQuaDangCho()
    {
        var h = new Harness();
        Assert.True(h.Sut.CanChangeMode); // Stopped

        h.Sut.Start();
        Assert.True(h.Sut.CanChangeMode); // Disconnected

        h.Link.OpenPort();
        h.Link.Receive("#HELLO,FW=1.0.0,PINS=16,GM=3A7F,BOOT=8F21C04D");
        h.Link.Receive("#PONG,ST=IDLE,SEQ=0");
        Assert.True(h.Sut.CanChangeMode); // Ready

        h.TestOk(105);
        Assert.False(h.Sut.CanChangeMode); // WaitScan

        await h.Sut.HandleScanAsync("TAG1");
        Assert.False(h.Sut.CanChangeMode); // Binding

        h.Link.Receive("#BIND_OK,SEQ=105");
        Assert.False(h.Sut.CanChangeMode); // Saving

        h.Link.Receive("#UNLOCKED,SEQ=105");
        Assert.True(h.Sut.CanChangeMode); // Ready lại
    }

    [Fact]
    public void MatKetNoiTrongLucChoPhanHoi_HuyLenhDangCho()
    {
        var h = new Harness();
        h.ToWaitScan(105);
        h.Sut.Discard("tt01");
        Assert.Equal(1, h.Scheduler.ActiveCount);

        h.Link.ClosePort();

        Assert.Equal(0, h.Scheduler.ActiveCount);
        Assert.Equal(TesterState.Disconnected, h.Sut.State);
    }

    [Fact]
    public void Changed_DuocRaiseKhiDoiTrangThai()
    {
        var h = new Harness();
        var count = 0;
        h.Sut.Changed += () => count++;

        h.Connect();
        h.TestOk();

        Assert.True(count >= 3);
    }

    [Fact]
    public void EventLogLoi_KhongChanThaoTac()
    {
        var h = new Harness();
        h.Api.OnEvent = _ => throw new InvalidOperationException("mất server");
        h.ToWaitScan(105);

        h.Link.Receive("#VOID,SEQ=105,REASON=REMOVED");

        Assert.Equal(TesterState.Ready, h.Sut.State);
    }
}
