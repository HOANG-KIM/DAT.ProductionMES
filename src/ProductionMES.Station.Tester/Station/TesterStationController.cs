using ProductionMES.Station.Tester.Protocol;

namespace ProductionMES.Station.Tester.Station;

/// <summary>
/// State machine "thông điện TRƯỚC, quét QR SAU" của App (US-12, US-14 AC5-AC8; spec-app mục 7.1/7.3/7.5). Nơi DUY NHẤT
/// ra quyết định — ViewModel chỉ hiển thị trạng thái và chuyển thao tác người dùng vào đây.
/// </summary>
/// <remarks>
/// Luồng xử lý: mọi sự kiện từ <see cref="ITesterLink"/> (luồng nền) được đưa qua <see cref="ITesterDispatcher"/> về luồng
/// duy nhất; mọi phương thức public khác cũng phải được gọi trên luồng đó (UI thread). Mỗi lúc chỉ có MỘT lệnh chờ phản
/// hồi (BIND/SAVED/DISCARD/RESULT?), timeout <see cref="TesterOptions.CommandTimeoutMs"/>, gửi lại tối đa 2 lần; PING do
/// <see cref="TesterLink"/> gửi độc lập.
///
/// Nguyên tắc an toàn dữ liệu: (1) chỉ gửi SAVED sau khi server đã commit; (2) mất kết nối máy => chặn quét, không lưu gì;
/// (3) mất server khi Saving => không SAVED, máy vẫn khóa, "Thử lại"; KHÔNG hàng đợi offline (US-12 AC14).
/// </remarks>
public sealed class TesterStationController : IDisposable
{
    /// <summary>Số lần gửi tối đa cho 1 lệnh: gửi đầu + tối đa 2 lần gửi lại (US-12 AC10).</summary>
    private const int MaxAttempts = 3;

    private readonly ITesterLink _link;
    private readonly ITesterServerApi _api;
    private readonly ITesterDispatcher _dispatcher;
    private readonly ITesterScheduler _scheduler;
    private readonly TesterOptions _options;
    private readonly ITesterLog _log;
    private readonly Func<DateTime> _now;

    private TesterState _state = TesterState.Stopped;
    private HeldResult? _held;
    private Pending? _pending;
    private int _generation;
    private bool _scanBusy;
    private bool _awaitingFirstPong;
    private bool _awaitBusyPong;
    private bool _linkLostLogged;
    private bool _disposed;
    private string? _lastResultGoldenCrc;
    private string? _discardApprover;
    private TesterState _stateBeforeDiscard = TesterState.WaitScan;

    public TesterStationController(
        ITesterLink link,
        ITesterServerApi api,
        ITesterDispatcher dispatcher,
        ITesterScheduler scheduler,
        TesterOptions options,
        ITesterLog log,
        Func<DateTime>? now = null)
    {
        _link = link;
        _api = api;
        _dispatcher = dispatcher;
        _scheduler = scheduler;
        _options = options;
        _log = log;
        _now = now ?? (() => DateTime.Now);

        _link.PortOpened += OnLinkPortOpened;
        _link.PortClosed += OnLinkPortClosed;
        _link.MessageReceived += OnLinkMessage;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Trạng thái quan sát được (đọc trên luồng xử lý)
    // ---------------------------------------------------------------------------------------------------------

    public TesterState State => _state;

    /// <summary>true khi đã đồng bộ xong với máy (không phải Stopped/Disconnected).</summary>
    public bool IsConnected => _state is not (TesterState.Stopped or TesterState.Disconnected);

    /// <summary>Phiên bản firmware (HELLO) — null nếu chưa nhận.</summary>
    public string? Firmware { get; private set; }

    /// <summary>Số pin (HELLO) — chỉ hiển thị/ghi log, chưa kiểm tra theo Model.</summary>
    public int? Pins { get; private set; }

    /// <summary>GM (CRC golden matrix) 4 ký tự hex — null nếu chưa nhận.</summary>
    public string? GoldenCrc { get; private set; }

    /// <summary>true khi máy chưa có mẫu (GM = 0000 hoặc PONG ST=NO_SAMPLE) — UI hiện banner "Máy chưa có mẫu — cần Learn" (US-14 AC8).</summary>
    public bool NoSample { get; private set; }

    /// <summary>BOOT id hiện tại của máy (HELLO gần nhất).</summary>
    public string? BootId { get; private set; }

    /// <summary>SEQ của kết quả OK đang giữ — null nếu không giữ kết quả nào.</summary>
    public int? HeldSeq => _held?.Seq;

    /// <summary>Mã tem đang xử lý (đã BIND) — null nếu chưa có.</summary>
    public string? HeldTagCode => _held?.TagCode;

    /// <summary>Chữ trạng thái lớn hiển thị trên AndonBoardWindow.</summary>
    public string StatusText => _state switch
    {
        TesterState.Stopped => string.Empty,
        TesterState.Disconnected => TesterTexts.Disconnected,
        TesterState.Ready => TesterTexts.Ready,
        TesterState.WaitScan => TesterTexts.WaitScan,
        TesterState.Binding => TesterTexts.Binding,
        TesterState.Saving => _held switch
        {
            { SaveFailed: true } => TesterTexts.SaveFailed,
            { SavedUnacked: true } => TesterTexts.NotUnlocked,
            _ => TesterTexts.Saving,
        },
        TesterState.Discarding => TesterTexts.Discarding,
        _ => string.Empty,
    };

    /// <summary>
    /// US-11 AC9: chỉ đổi được công tắc khi KHÔNG có kết quả đang chờ (Stopped/Disconnected/Ready). Ở WaitScan/Binding/Saving/
    /// Discarding phải hủy kết quả trước.
    /// </summary>
    public bool CanChangeMode => _state is TesterState.Stopped or TesterState.Disconnected or TesterState.Ready;

    /// <summary>"Hủy kết quả" (cần Tổ trưởng): ở WaitScan, hoặc ở Saving khi lưu lỗi (AC11).</summary>
    public bool CanDiscard => _pending is null && _held is { } h
        && (_state == TesterState.WaitScan
            || (_state == TesterState.Saving && h.SaveFailed && !h.DbSaved && !h.SaveInFlight));

    /// <summary>"Thử lại" ở Saving khi lưu lỗi / chưa nhận UNLOCKED (AC13).</summary>
    public bool CanRetrySave => _state == TesterState.Saving && _pending is null && _held is { } h
        && !h.SaveInFlight && (h.SaveFailed || h.SavedUnacked);

    /// <summary>Mọi thay đổi quan sát được (state, firmware, GM, cờ nút...) — UI đọc lại toàn bộ.</summary>
    public event Action? Changed;

    /// <summary>Thông báo cho người vận hành (DÂY NG, Chưa thông điện, Đã lưu...).</summary>
    public event Action<TesterNotice>? NoticeRaised;

    /// <summary>Scan đã commit lên server (sau BIND_OK) — <c>payload</c> là dữ liệu thô của client (vd <c>ScanResultDto</c>) để UI cập nhật bộ đếm thùng.</summary>
    public event Action<string, object?>? ScanSaved;

    // ---------------------------------------------------------------------------------------------------------
    // Điều khiển vòng đời
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>Bật chế độ dùng máy: mở cổng COM, PING, chờ đồng bộ. Idempotent.</summary>
    public void Start()
    {
        if (_disposed || _state != TesterState.Stopped)
        {
            return;
        }

        _generation++;
        _held = null;
        ClearPending();
        _awaitingFirstPong = true;
        _awaitBusyPong = false;
        _linkLostLogged = false;
        _scanBusy = false;
        SetState(TesterState.Disconnected);
        _link.Start();
    }

    /// <summary>Tắt chế độ dùng máy (công tắc TẮT): đóng cổng COM, ngừng PING (US-11 AC7). Kết quả đang giữ coi như bỏ. Idempotent.</summary>
    public void Stop()
    {
        if (_state == TesterState.Stopped)
        {
            return;
        }

        _generation++;
        _link.Stop();
        ClearPending();
        _held = null;
        _awaitingFirstPong = false;
        _awaitBusyPong = false;
        _scanBusy = false;
        SetState(TesterState.Stopped);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _link.PortOpened -= OnLinkPortOpened;
        _link.PortClosed -= OnLinkPortClosed;
        _link.MessageReceived -= OnLinkMessage;
        Stop();
        _link.Dispose();
    }

    // ---------------------------------------------------------------------------------------------------------
    // Thao tác của người dùng
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Mã QR vừa quét (US-12). Ở <c>Ready</c>: gửi <c>#BIND,SEQ=0</c> NGAY, KHÔNG gọi server kiểm tra tem (AC7/AC8). Ở
    /// <c>WaitScan</c>: kiểm tra tem trên server (FR-08, không lưu); hợp lệ -> BIND; bị từ chối -> KHÔNG BIND, máy vẫn khóa (AC3).
    /// </summary>
    public async Task<TesterScanOutcome> HandleScanAsync(string tagCode)
    {
        if (_state == TesterState.Stopped)
        {
            return new TesterScanOutcome(TesterScanOutcomeKind.Blocked, TesterTexts.ScanBlockedStopped);
        }

        if (_state == TesterState.Disconnected)
        {
            return new TesterScanOutcome(TesterScanOutcomeKind.Blocked, TesterTexts.ScanBlockedDisconnected);
        }

        if (_scanBusy || _pending is not null)
        {
            return new TesterScanOutcome(TesterScanOutcomeKind.Busy, TesterTexts.ScanBusy);
        }

        switch (_state)
        {
            case TesterState.Ready:
                // Không giữ kết quả OK: hỏi thẳng máy để nhận đúng lý do (DÂY NG / Chưa thông điện / Đang thông điện...).
                StartPending(PendingKind.BindAtReady, TesterCommand.Bind(0), tagCode);
                return new TesterScanOutcome(TesterScanOutcomeKind.SentToMachine);

            case TesterState.WaitScan:
                return await CheckAndBindAsync(tagCode);

            default:
                return new TesterScanOutcome(TesterScanOutcomeKind.Busy, TesterTexts.ScanBusy);
        }
    }

    /// <summary>"Hủy kết quả" đã được Tổ trưởng duyệt (AC11): gửi <c>#DISCARD</c>. Trả false nếu lúc này không hủy được.</summary>
    public bool Discard(string approvedBy)
    {
        if (!CanDiscard)
        {
            return false;
        }

        _discardApprover = approvedBy;
        _stateBeforeDiscard = _state;
        SetState(TesterState.Discarding);
        StartPending(PendingKind.Discard, TesterCommand.Discard(_held!.Seq), _held.TagCode);
        return true;
    }

    /// <summary>"Thử lại" (AC13): lưu lại lên server nếu chưa commit, hoặc gửi lại SAVED nếu đã commit mà chưa nhận UNLOCKED.</summary>
    public bool RetrySave()
    {
        if (!CanRetrySave)
        {
            return false;
        }

        var held = _held!;
        held.SaveFailed = false;
        held.SavedUnacked = false;
        if (held.DbSaved)
        {
            SendSaved(held);
        }
        else
        {
            StartSave(held);
        }

        RaiseChanged();
        return true;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Quét tem ở WaitScan
    // ---------------------------------------------------------------------------------------------------------

    private async Task<TesterScanOutcome> CheckAndBindAsync(string tagCode)
    {
        var held = _held!;
        var generation = _generation;
        _scanBusy = true;

        TagCheckResult check;
        try
        {
            check = await _api.CheckTagAsync(tagCode, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _scanBusy = false;
            _log.Warn($"Kiểm tra tem '{tagCode}' lỗi: {ex.Message}");
            return new TesterScanOutcome(TesterScanOutcomeKind.ServerError, ex.Message);
        }

        _scanBusy = false;

        // Trong lúc chờ server: có thể rút dây (VOID), mất kết nối, tắt công tắc... => không BIND.
        if (generation != _generation || _state != TesterState.WaitScan || !ReferenceEquals(_held, held) || _pending is not null)
        {
            return new TesterScanOutcome(TesterScanOutcomeKind.Aborted, TesterTexts.StateChanged);
        }

        if (!check.Accepted)
        {
            return new TesterScanOutcome(TesterScanOutcomeKind.Rejected, check.RejectionReason ?? string.Empty, check.Payload);
        }

        held.TagCode = tagCode;
        SetState(TesterState.Binding);
        StartPending(PendingKind.BindHeld, TesterCommand.Bind(held.Seq), tagCode);
        return new TesterScanOutcome(TesterScanOutcomeKind.Bound);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Sự kiện từ link (luồng nền -> luồng xử lý duy nhất)
    // ---------------------------------------------------------------------------------------------------------

    private void OnLinkPortOpened() => _dispatcher.Post(HandlePortOpened);

    private void OnLinkPortClosed(string reason) => _dispatcher.Post(() => HandlePortClosed(reason));

    private void OnLinkMessage(TesterMessage message) => _dispatcher.Post(() => HandleMessage(message));

    private void HandlePortOpened()
    {
        if (_state == TesterState.Stopped)
        {
            return;
        }

        // Cổng vừa (mở lại) mở: chờ HELLO + PONG đầu tiên rồi mới đồng bộ (spec-app 7.3). TesterLink đã gửi #HELLO?.
        _awaitingFirstPong = true;
        ClearPending();
    }

    private void HandlePortClosed(string reason)
    {
        if (_state == TesterState.Stopped)
        {
            return;
        }

        var wasConnected = _state != TesterState.Disconnected;
        ClearPending();
        _awaitBusyPong = false;
        _awaitingFirstPong = true;
        SetState(TesterState.Disconnected);

        if (wasConnected)
        {
            // US-14 AC3/AC6: giữ kết quả (nếu có) trong bộ nhớ, chờ đồng bộ lại; ghi event, chặn quét.
            _linkLostLogged = true;
            RecordEvent(new TesterEvent(TesterEventType.LinkLost, Seq: _held?.Seq, TagCode: _held?.TagCode, Detail: reason));
            Notice(TesterNoticeKind.LinkLost, TesterTexts.LinkLost);
        }
    }

    private void HandleMessage(TesterMessage message)
    {
        if (_disposed || _state == TesterState.Stopped)
        {
            return;
        }

        switch (message)
        {
            case HelloMessage hello:
                HandleHello(hello);
                break;
            case PongMessage pong:
                HandlePong(pong);
                break;
            case ResultMessage result:
                HandleResult(result);
                break;
            case BindOkMessage bindOk:
                HandleBindOk(bindOk);
                break;
            case BindFailMessage bindFail:
                HandleBindFail(bindFail);
                break;
            case UnlockedMessage unlocked:
                HandleUnlocked(unlocked);
                break;
            case VoidMessage voided:
                HandleVoid(voided);
                break;
            case ErrorMessage error:
                HandleError(error);
                break;
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // HELLO / PONG / đồng bộ lại
    // ---------------------------------------------------------------------------------------------------------

    private void HandleHello(HelloMessage hello)
    {
        var previousBoot = BootId;
        Firmware = hello.Firmware;
        Pins = hello.Pins;
        BootId = hello.BootId;
        GoldenCrc = hello.GoldenCrc;
        NoSample = IsEmptyGoldenCrc(hello.GoldenCrc);
        _log.Info($"HELLO: FW={hello.Firmware} PINS={hello.Pins} GM={hello.GoldenCrc} BOOT={hello.BootId}");

        if (previousBoot is not null && !string.Equals(previousBoot, hello.BootId, StringComparison.OrdinalIgnoreCase))
        {
            // Máy đã reset: kết quả đang chờ (nếu có) mất (US-14 AC5).
            if (_held is { } held)
            {
                RecordEvent(new TesterEvent(TesterEventType.VoidReboot, held.TagCode, held.Seq, $"BOOT {previousBoot} -> {hello.BootId}"));
                ClearPending();
                _awaitBusyPong = false;
                _held = null;
                if (_state is TesterState.WaitScan or TesterState.Binding or TesterState.Saving or TesterState.Discarding)
                {
                    SetState(TesterState.Ready);
                }

                Notice(TesterNoticeKind.MachineReset, TesterTexts.MachineReset);
            }
        }

        RaiseChanged();
    }

    private void HandlePong(PongMessage pong)
    {
        // Cờ "chưa có mẫu" theo PONG: ST=NO_SAMPLE bật; trạng thái khác thì giữ theo GM đã biết (HELLO/RESULT).
        if (pong.State == MachineState.NoSample)
        {
            NoSample = true;
        }
        else if (GoldenCrc is not null)
        {
            NoSample = IsEmptyGoldenCrc(GoldenCrc);
        }

        if (_awaitingFirstPong)
        {
            _awaitingFirstPong = false;
            SyncAfterConnect(pong);
            return;
        }

        if (_awaitBusyPong && _state == TesterState.Binding && _held is { } held)
        {
            _awaitBusyPong = false;
            ReconcileAfterBusy(pong, held);
            return;
        }

        RaiseChanged();
    }

    /// <summary>Đồng bộ lại theo PONG đầu tiên sau khi (tái) kết nối — bảng spec-app 7.3 / US-14 AC5.</summary>
    private void SyncAfterConnect(PongMessage pong)
    {
        if (_linkLostLogged)
        {
            _linkLostLogged = false;
            RecordEvent(new TesterEvent(TesterEventType.LinkRestored, Detail: $"FW={Firmware} BOOT={BootId}"));
            Notice(TesterNoticeKind.LinkRestored, TesterTexts.LinkRestored);
        }

        var held = _held;
        var machineSeq = pong.Seq;

        if (pong.State == MachineState.WaitBind && machineSeq > 0)
        {
            if (held is not null && held.Seq == machineSeq)
            {
                // Máy vẫn giữ đúng kết quả App giữ -> tiếp tục WaitScan.
                SetState(TesterState.WaitScan);
                return;
            }

            // Máy có kết quả mà App không giữ (hoặc giữ cái khác) -> lấy lại bằng #RESULT?.
            ReplaceHeldAndRecover(held, "máy giữ kết quả khác SEQ");
            return;
        }

        if (pong.State == MachineState.Binding && machineSeq > 0)
        {
            if (held is not null && held.Seq == machineSeq)
            {
                // Máy đã BIND_OK: coi như App đã nhận BIND_OK (BIND_OK có thể rơi mất đúng lúc mất link) -> tiếp tục Saving.
                held.BindAcked = true;
                SetState(TesterState.Saving);
                ResumeSaving(held);
                return;
            }

            ReplaceHeldAndRecover(held, "máy đang chờ SAVED cho kết quả khác SEQ");
            return;
        }

        // SEQ = 0 (hoặc trạng thái không liên quan tới kết quả đang chờ): máy không giữ kết quả nào.
        if (held is not null)
        {
            _held = null;
            if (held.DbSaved)
            {
                // US-12 AC16: đã commit lên server nhưng mất kết nối trước UNLOCKED — bản ghi vẫn hợp lệ (đã có BIND_OK),
                // máy tự mở khóa sau khi mất link quá 5 giây. Không phải sự cố, không ghi VOID.
                Notice(TesterNoticeKind.Saved, TesterTexts.Saved(held.TagCode ?? string.Empty), held.TagCode);
            }
            else
            {
                // Máy đã hủy (mất link quá lâu) -> kết quả của App không còn hiệu lực.
                RecordEvent(new TesterEvent(TesterEventType.VoidLinkLost, held.TagCode, held.Seq, "Máy không còn giữ kết quả sau khi kết nối lại."));
                Notice(TesterNoticeKind.ResultVoidedByMachine, TesterTexts.ResultVoidedByMachine);
            }
        }

        SetState(TesterState.Ready);
    }

    private void ReplaceHeldAndRecover(HeldResult? previousHeld, string detail)
    {
        if (previousHeld is not null)
        {
            RecordEvent(new TesterEvent(TesterEventType.VoidLinkLost, previousHeld.TagCode, previousHeld.Seq, $"Kết quả cũ bị thay thế: {detail}."));
            _held = null;
        }

        SetState(TesterState.Ready);
        StartPending(PendingKind.ResultQuery, TesterCommand.ResultQuery());
    }

    /// <summary>AC10: sau BUSY ở Binding, đối chiếu PONG đầu tiên để biết máy đang ở đâu.</summary>
    private void ReconcileAfterBusy(PongMessage pong, HeldResult held)
    {
        if (pong.State == MachineState.Binding && pong.Seq == held.Seq)
        {
            held.BindAcked = true;
            SetState(TesterState.Saving);
            ResumeSaving(held);
            return;
        }

        if (pong.State == MachineState.WaitBind && pong.Seq == held.Seq)
        {
            // BIND lần trước chưa được xử lý: về WaitScan để quét lại.
            SetState(TesterState.WaitScan);
            Notice(TesterNoticeKind.ResultRecovered, TesterTexts.ResultRecovered, held.TagCode);
            return;
        }

        RecordEvent(new TesterEvent(TesterEventType.BindFail, held.TagCode, held.Seq, $"Sau BUSY máy báo ST={pong.RawState},SEQ={pong.Seq} — mất kết quả."));
        _held = null;
        SetState(TesterState.Ready);
        Notice(TesterNoticeKind.WireRemoved, TesterTexts.WireRemoved);
    }

    // ---------------------------------------------------------------------------------------------------------
    // RESULT / BIND_OK / BIND_FAIL / UNLOCKED / VOID / ERR
    // ---------------------------------------------------------------------------------------------------------

    private void HandleResult(ResultMessage result)
    {
        if (_state == TesterState.Disconnected)
        {
            // Chưa đồng bộ xong: bỏ qua, bước đồng bộ (PONG đầu tiên) sẽ gửi #RESULT? nếu máy đang giữ kết quả.
            _log.Debug($"Bỏ qua RESULT SEQ={result.Seq} khi chưa đồng bộ.");
            return;
        }

        if (!string.Equals(result.Result, "OK", StringComparison.OrdinalIgnoreCase))
        {
            _log.Warn($"RESULT với R={result.Result} (firmware chỉ gửi OK) — bỏ qua.");
            return;
        }

        if (_lastResultGoldenCrc is not null && !string.Equals(_lastResultGoldenCrc, result.GoldenCrc, StringComparison.OrdinalIgnoreCase))
        {
            _log.Info($"Mẫu (golden matrix) đã thay đổi: {_lastResultGoldenCrc} -> {result.GoldenCrc}");
        }

        _lastResultGoldenCrc = result.GoldenCrc;
        GoldenCrc = result.GoldenCrc;
        NoSample = IsEmptyGoldenCrc(result.GoldenCrc);

        switch (_state)
        {
            case TesterState.Ready:
            case TesterState.WaitScan:
                var recovered = _pending?.Kind == PendingKind.ResultQuery;
                if (recovered)
                {
                    ClearPending();
                }

                if (_held is null || _held.Seq != result.Seq)
                {
                    if (BootId is null)
                    {
                        // Chưa biết BOOT (HELLO rơi mất): hỏi lại để có bộ khóa (BOOT, SEQ) chống lưu trùng.
                        _link.Send(TesterCommand.HelloQuery());
                    }

                    _held = new HeldResult(result.Seq, result.GoldenCrc, BootId ?? string.Empty, _now());
                }

                SetState(TesterState.WaitScan);
                if (recovered)
                {
                    Notice(TesterNoticeKind.ResultRecovered, TesterTexts.ResultRecovered);
                }

                break;

            default:
                // Binding/Saving/Discarding: máy không thể sinh kết quả mới; RESULT trùng SEQ (gửi lại) hoặc lạ -> bỏ qua.
                _log.Debug($"Bỏ qua RESULT SEQ={result.Seq} khi đang ở {_state}.");
                break;
        }
    }

    private void HandleBindOk(BindOkMessage bindOk)
    {
        var held = _held;
        if (held is null || held.Seq != bindOk.Seq)
        {
            _log.Warn($"BIND_OK SEQ={bindOk.Seq} không khớp kết quả đang giữ ({held?.Seq.ToString() ?? "không có"}) — bỏ qua.");
            return;
        }

        // BIND_OK hợp lệ khi đang Binding, hoặc "muộn" (hết lượt gửi lại, App đã về WaitScan) miễn đã có tem (BIND đã gửi).
        if (_state is not (TesterState.Binding or TesterState.WaitScan) || held.TagCode is null)
        {
            _log.Warn($"BIND_OK SEQ={bindOk.Seq} ngoài dự kiến ở trạng thái {_state} — bỏ qua.");
            return;
        }

        if (_pending?.Kind == PendingKind.BindHeld)
        {
            ClearPending();
        }

        _awaitBusyPong = false;
        held.BindAcked = true;
        SetState(TesterState.Saving);
        StartSave(held);
    }

    private void HandleBindFail(BindFailMessage fail)
    {
        var pending = _pending;
        if (pending is null || pending.Kind is not (PendingKind.BindAtReady or PendingKind.BindHeld))
        {
            _log.Warn($"BIND_FAIL REASON={fail.RawReason} khi không chờ BIND — bỏ qua.");
            return;
        }

        if (pending.Kind == PendingKind.BindAtReady)
        {
            HandleBindFailAtReady(fail, pending);
            return;
        }

        HandleBindFailWhileBinding(fail, pending);
    }

    private void HandleBindFailAtReady(BindFailMessage fail, Pending pending)
    {
        var tag = pending.Tag;
        switch (fail.Reason)
        {
            case BindFailReason.Busy:
                // Máy báo bận: gửi lại tối đa 2 lần rồi báo.
                if (pending.Attempts < MaxAttempts)
                {
                    pending.Attempts++;
                    _link.Send(pending.Command);
                    ArmTimer(pending);
                    return;
                }

                ClearPending();
                RecordEvent(new TesterEvent(TesterEventType.BindFail, tag, Detail: "BUSY"));
                Notice(TesterNoticeKind.BindBusy, TesterTexts.BindBusyFinal, tag);
                return;

            case BindFailReason.Ng:
                ClearPending();
                RecordEvent(new TesterEvent(TesterEventType.ScanOnNg, tag, Detail: "Quét QR khi dây NG còn trên jig."));
                Notice(TesterNoticeKind.WireNg, TesterTexts.WireNg, tag);
                return;

            case BindFailReason.NoPending:
            case BindFailReason.Removed:
                ClearPending();
                Notice(TesterNoticeKind.NotTested, TesterTexts.NotTested, tag);
                return;

            case BindFailReason.Testing:
                ClearPending();
                Notice(TesterNoticeKind.Testing, TesterTexts.Testing, tag);
                return;

            case BindFailReason.BadSeq:
                // Máy đang giữ kết quả OK mà App lỡ mất: lấy lại bằng #RESULT? rồi báo quét lại (AC9).
                ClearPending();
                RecordEvent(new TesterEvent(TesterEventType.BindFail, tag, Detail: "BAD_SEQ"));
                StartPending(PendingKind.ResultQuery, TesterCommand.ResultQuery());
                return;

            default:
                ClearPending();
                RecordEvent(new TesterEvent(TesterEventType.BindFail, tag, Detail: fail.RawReason));
                Notice(TesterNoticeKind.MachineError, TesterTexts.MachineError(fail.RawReason), tag);
                return;
        }
    }

    private void HandleBindFailWhileBinding(BindFailMessage fail, Pending pending)
    {
        var held = _held!;
        var tag = pending.Tag ?? held.TagCode;
        switch (fail.Reason)
        {
            case BindFailReason.NoPending:
            case BindFailReason.Removed:
                // AC6: dây đã rút sát lúc BIND -> không lưu, về Ready.
                ClearPending();
                RecordEvent(new TesterEvent(TesterEventType.VoidRemoved, tag, held.Seq, $"BIND_FAIL {fail.RawReason}"));
                _held = null;
                SetState(TesterState.Ready);
                Notice(TesterNoticeKind.WireRemoved, TesterTexts.WireRemoved, tag);
                return;

            case BindFailReason.Busy:
                // AC10: máy đã BIND rồi / hàng chờ đầy. Không gửi trùng; đợi PONG để đồng bộ.
                ClearPending();
                _awaitBusyPong = true;
                Notice(TesterNoticeKind.BindBusy, TesterTexts.BindBusy, tag);
                return;

            case BindFailReason.BadSeq:
                ClearPending();
                RecordEvent(new TesterEvent(TesterEventType.BindFail, tag, held.Seq, "BAD_SEQ"));
                SetState(TesterState.WaitScan);
                StartPending(PendingKind.ResultQuery, TesterCommand.ResultQuery());
                return;

            default:
                ClearPending();
                RecordEvent(new TesterEvent(TesterEventType.BindFail, tag, held.Seq, fail.RawReason));
                SetState(TesterState.WaitScan);
                Notice(TesterNoticeKind.MachineError, TesterTexts.MachineError(fail.RawReason), tag);
                return;
        }
    }

    private void HandleUnlocked(UnlockedMessage unlocked)
    {
        var held = _held;
        if (held is null || held.Seq != unlocked.Seq)
        {
            _log.Warn($"UNLOCKED SEQ={unlocked.Seq} không khớp kết quả đang giữ — bỏ qua.");
            return;
        }

        if (_pending?.Kind == PendingKind.Saved)
        {
            ClearPending();
        }

        _held = null;
        ReturnToReady();
        Notice(TesterNoticeKind.Saved, TesterTexts.Saved(held.TagCode ?? string.Empty), held.TagCode);
    }

    private void HandleVoid(VoidMessage voided)
    {
        var held = _held;
        if (held is null)
        {
            _log.Debug($"VOID {voided.RawReason} khi không giữ kết quả — bỏ qua.");
            return;
        }

        ClearPending();
        _awaitBusyPong = false;
        _held = null;

        switch (voided.Reason)
        {
            case VoidReason.Discard:
                RecordEvent(new TesterEvent(TesterEventType.VoidDiscard, held.TagCode, held.Seq, "Tổ trưởng hủy kết quả.", _discardApprover));
                _discardApprover = null;
                ReturnToReady();
                Notice(TesterNoticeKind.Discarded, TesterTexts.Discarded, held.TagCode);
                break;

            case VoidReason.Removed:
                RecordEvent(new TesterEvent(TesterEventType.VoidRemoved, held.TagCode, held.Seq, "Dây bị rút."));
                ReturnToReady();
                Notice(TesterNoticeKind.WireRemoved, TesterTexts.WireRemoved, held.TagCode);
                break;

            default:
                RecordEvent(new TesterEvent(TesterEventType.VoidRemoved, held.TagCode, held.Seq, $"VOID {voided.RawReason}"));
                ReturnToReady();
                Notice(TesterNoticeKind.WireRemoved, TesterTexts.WireRemoved, held.TagCode);
                break;
        }
    }

    private void HandleError(ErrorMessage error)
    {
        _log.Warn($"ERR CODE={error.RawCode}");

        var pending = _pending;
        if (pending is null)
        {
            RaiseChanged();
            return;
        }

        switch (pending.Kind)
        {
            case PendingKind.BindAtReady:
            case PendingKind.BindHeld:
                if (error.Code == ErrorCode.Busy)
                {
                    // Cùng ý nghĩa BIND_FAIL,REASON=BUSY.
                    HandleBindFail(new BindFailMessage(null, BindFailReason.Busy, "BUSY"));
                    return;
                }

                // Lệnh sai / quá dài...: không còn gì để chờ.
                var tag = pending.Tag;
                ClearPending();
                RecordEvent(new TesterEvent(TesterEventType.BindFail, tag, _held?.Seq, $"ERR {error.RawCode}"));
                if (_state == TesterState.Binding)
                {
                    SetState(TesterState.WaitScan);
                }

                Notice(TesterNoticeKind.MachineError, TesterTexts.MachineError(error.RawCode), tag);
                return;

            case PendingKind.Saved:
                if (error.Code is ErrorCode.NoPending or ErrorCode.BadSeq)
                {
                    // Máy không còn chờ SAVED (đã hủy/rút dây) mà bản ghi đã commit -> kết thúc, bản ghi vẫn hợp lệ.
                    ClearPending();
                    var held = _held;
                    _held = null;
                    ReturnToReady();
                    Notice(TesterNoticeKind.Saved, TesterTexts.Saved(held?.TagCode ?? string.Empty), held?.TagCode);
                }

                return;

            case PendingKind.Discard:
                if (error.Code is ErrorCode.NoPending or ErrorCode.BadSeq)
                {
                    ClearPending();
                    _held = null;
                    _discardApprover = null;
                    ReturnToReady();
                    Notice(TesterNoticeKind.Discarded, TesterTexts.DiscardGone);
                }

                return;

            case PendingKind.ResultQuery:
                if (error.Code is ErrorCode.NoPending or ErrorCode.BadSeq)
                {
                    // Máy không còn kết quả nào (vừa bị hủy/rút dây).
                    ClearPending();
                    _held = null;
                    ReturnToReady();
                }

                return;
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Lưu lên server (Saving) + SAVED
    // ---------------------------------------------------------------------------------------------------------

    private void StartSave(HeldResult held)
    {
        if (held.TagCode is null)
        {
            // Phòng vệ: không thể ở Saving mà chưa có tem (BIND_OK chỉ đến sau khi đã BIND với tem).
            held.SaveFailed = true;
            held.SaveError = "Chưa có mã tem để lưu.";
            RaiseChanged();
            return;
        }

        held.SaveInFlight = true;
        held.SaveFailed = false;
        var generation = _generation;
        var save = new TesterScanSave(
            held.TagCode, string.IsNullOrEmpty(held.BootId) ? BootId ?? string.Empty : held.BootId, held.Seq, held.GoldenCrc, Firmware ?? string.Empty, held.ReceivedAt);

        _ = RunSaveAsync(held, generation, save);
        RaiseChanged();
    }

    private async Task RunSaveAsync(HeldResult held, int generation, TesterScanSave save)
    {
        TesterSaveResult? result = null;
        Exception? error = null;
        try
        {
            result = await _api.SaveTesterScanAsync(save, CancellationToken.None);
        }
        catch (Exception ex)
        {
            error = ex;
        }

        _dispatcher.Post(() => OnSaveCompleted(held, generation, result, error));
    }

    private void OnSaveCompleted(HeldResult held, int generation, TesterSaveResult? result, Exception? error)
    {
        held.SaveInFlight = false;
        if (generation != _generation)
        {
            // Công tắc đã tắt/bật lại trong lúc lưu: kết quả lưu (nếu thành công) vẫn hợp lệ trên server, không còn gì để làm ở máy.
            return;
        }

        if (error is not null || result is null || !result.Accepted)
        {
            var message = error?.Message ?? result?.RejectionReason ?? "Server từ chối lưu.";
            held.SaveFailed = true;
            held.SaveError = message;
            _log.Warn($"Lưu Scan SEQ={held.Seq} tem '{held.TagCode}' thất bại: {message}");
            RecordEvent(new TesterEvent(TesterEventType.DbError, held.TagCode, held.Seq, message));
            if (ReferenceEquals(_held, held))
            {
                // AC13: KHÔNG gửi SAVED — máy vẫn khóa, dây còn trên jig. Nút "Thử lại" / Tổ trưởng "Hủy".
                Notice(TesterNoticeKind.SaveFailed, $"Lưu lên server thất bại: {message}", held.TagCode);
            }

            return;
        }

        held.DbSaved = true;
        held.SaveFailed = false;
        ScanSaved?.Invoke(held.TagCode ?? string.Empty, result.Payload);

        if (!ReferenceEquals(_held, held))
        {
            // Dây bị rút/máy reset trong lúc lưu: bản ghi đã commit vẫn hợp lệ (đã có BIND_OK), không còn SAVED để gửi.
            Notice(TesterNoticeKind.Saved, TesterTexts.Saved(held.TagCode ?? string.Empty), held.TagCode);
            return;
        }

        if (_state == TesterState.Disconnected)
        {
            // AC16/US-14 AC6: đã commit nhưng mất kết nối máy — chờ đồng bộ lại (ResumeSaving) hoặc máy tự mở khóa sau 5 giây.
            RaiseChanged();
            return;
        }

        // CHỈ sau khi server commit mới gửi SAVED.
        SendSaved(held);
    }

    private void SendSaved(HeldResult held)
    {
        StartPending(PendingKind.Saved, TesterCommand.Saved(held.Seq), held.TagCode);
    }

    /// <summary>Sau khi đồng bộ vào Saving (máy đang BINDING): gửi SAVED nếu đã commit, lưu nếu chưa; không làm gì nếu đang lưu dở.</summary>
    private void ResumeSaving(HeldResult held)
    {
        if (held.SaveInFlight)
        {
            return;
        }

        if (held.DbSaved)
        {
            SendSaved(held);
        }
        else
        {
            StartSave(held);
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Lệnh chờ phản hồi + timeout
    // ---------------------------------------------------------------------------------------------------------

    private enum PendingKind
    {
        BindAtReady,
        BindHeld,
        Saved,
        Discard,
        ResultQuery,
    }

    private sealed class Pending
    {
        public PendingKind Kind { get; init; }

        public TesterCommand Command { get; init; }

        public string? Tag { get; init; }

        public int Attempts { get; set; } = 1;

        public IDisposable? Timer { get; set; }
    }

    private void StartPending(PendingKind kind, TesterCommand command, string? tag = null)
    {
        ClearPending();
        var pending = new Pending { Kind = kind, Command = command, Tag = tag };
        _pending = pending;
        _link.Send(command);
        ArmTimer(pending);
        RaiseChanged();
    }

    private void ArmTimer(Pending pending)
    {
        pending.Timer?.Dispose();
        pending.Timer = _scheduler.Schedule(TimeSpan.FromMilliseconds(_options.CommandTimeoutMs), () => OnPendingTimeout(pending));
    }

    private void ClearPending()
    {
        _pending?.Timer?.Dispose();
        _pending = null;
    }

    private void OnPendingTimeout(Pending pending)
    {
        if (!ReferenceEquals(_pending, pending))
        {
            return;
        }

        if (_state is TesterState.Stopped or TesterState.Disconnected)
        {
            ClearPending();
            return;
        }

        if (pending.Attempts < MaxAttempts)
        {
            pending.Attempts++;
            _log.Warn($"Lệnh {pending.Command.ToLine()} không phản hồi — gửi lại lần {pending.Attempts}/{MaxAttempts}.");
            _link.Send(pending.Command);
            ArmTimer(pending);
            return;
        }

        _log.Warn($"Lệnh {pending.Command.ToLine()} không phản hồi sau {MaxAttempts} lần gửi.");
        ClearPending();

        switch (pending.Kind)
        {
            case PendingKind.BindAtReady:
                RecordEvent(new TesterEvent(TesterEventType.BindFail, pending.Tag, Detail: "Không phản hồi (timeout)."));
                Notice(TesterNoticeKind.NoResponse, TesterTexts.NoResponse, pending.Tag);
                break;

            case PendingKind.BindHeld:
                RecordEvent(new TesterEvent(TesterEventType.BindFail, pending.Tag, _held?.Seq, "Không phản hồi (timeout)."));
                if (_state == TesterState.Binding)
                {
                    SetState(TesterState.WaitScan);
                }

                Notice(TesterNoticeKind.NoResponse, TesterTexts.NoResponse, pending.Tag);
                break;

            case PendingKind.Saved:
                if (_held is { } held && _state == TesterState.Saving)
                {
                    held.SavedUnacked = true;
                    Notice(TesterNoticeKind.SaveFailed, TesterTexts.NotUnlockedNotice, held.TagCode);
                }

                break;

            case PendingKind.Discard:
                if (_state == TesterState.Discarding)
                {
                    SetState(_stateBeforeDiscard);
                }

                _discardApprover = null;
                Notice(TesterNoticeKind.NoResponse, TesterTexts.NoResponse);
                break;

            case PendingKind.ResultQuery:
                Notice(TesterNoticeKind.NoResponse, TesterTexts.NoResponse);
                break;
        }

        RaiseChanged();
    }

    // ---------------------------------------------------------------------------------------------------------
    // Tiện ích
    // ---------------------------------------------------------------------------------------------------------

    private sealed class HeldResult
    {
        public HeldResult(int seq, string goldenCrc, string bootId, DateTime receivedAt)
        {
            Seq = seq;
            GoldenCrc = goldenCrc;
            BootId = bootId;
            ReceivedAt = receivedAt;
        }

        public int Seq { get; }

        public string GoldenCrc { get; }

        public string BootId { get; }

        public DateTime ReceivedAt { get; }

        public string? TagCode { get; set; }

        public bool BindAcked { get; set; }

        public bool SaveInFlight { get; set; }

        public bool DbSaved { get; set; }

        public bool SaveFailed { get; set; }

        public bool SavedUnacked { get; set; }

        public string? SaveError { get; set; }
    }

    private static bool IsEmptyGoldenCrc(string goldenCrc)
        => string.Equals(goldenCrc, "0000", StringComparison.Ordinal);

    /// <summary>
    /// Về <c>Ready</c> sau khi kết quả kết thúc (VOID/UNLOCKED/ERR...). Nếu đang <c>Disconnected</c> (chưa đồng bộ xong) thì GIỮ
    /// NGUYÊN — không được mở khóa quét trước khi PONG đầu tiên xác nhận máy (US-14 AC3/AC5).
    /// </summary>
    private void ReturnToReady()
    {
        if (_state == TesterState.Disconnected)
        {
            RaiseChanged();
            return;
        }

        SetState(TesterState.Ready);
    }

    private void SetState(TesterState state)
    {
        _state = state;
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        try
        {
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            _log.Error("Lỗi trong handler Changed của TesterStationController.", ex);
        }
    }

    private void Notice(TesterNoticeKind kind, string message, string? tagCode = null)
    {
        _log.Info($"NOTICE {kind}: {message}");
        try
        {
            NoticeRaised?.Invoke(new TesterNotice(kind, message, tagCode));
        }
        catch (Exception ex)
        {
            _log.Error("Lỗi trong handler NoticeRaised của TesterStationController.", ex);
        }
    }

    /// <summary>Ghi event log lên server (fire-and-forget). Lỗi (mất server...) chỉ ghi file, KHÔNG chặn thao tác (US-14 AC12).</summary>
    private void RecordEvent(TesterEvent testerEvent)
    {
        _log.Info($"EVENT {testerEvent.Type} tag={testerEvent.TagCode} seq={testerEvent.Seq} detail={testerEvent.Detail} by={testerEvent.ApprovedBy}");
        _ = RecordEventSafeAsync(testerEvent);
    }

    private async Task RecordEventSafeAsync(TesterEvent testerEvent)
    {
        try
        {
            await _api.RecordEventAsync(testerEvent, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.Warn($"Không ghi được event log {testerEvent.Type} lên server: {ex.Message} — chỉ ghi file.");
        }
    }
}
