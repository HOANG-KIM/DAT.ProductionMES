namespace ProductionMES.Station.Tester.Station;

/// <summary>
/// Chuyển mọi lời gọi về LUỒNG XỬ LÝ DUY NHẤT (Dispatcher của WPF) — <see cref="TesterStationController"/> không thread-safe,
/// mọi chuyển trạng thái phải chạy trên cùng 1 luồng (spec-app mục 5.2).
/// </summary>
public interface ITesterDispatcher
{
    void Post(Action action);
}

/// <summary>Hẹn giờ cho timeout lệnh. <paramref name="callback"/> phải được chạy trên luồng xử lý duy nhất.</summary>
public interface ITesterScheduler
{
    IDisposable Schedule(TimeSpan delay, Action callback);
}

/// <summary>Kết quả kiểm tra tem trên server (FR-08) — <see cref="Payload"/> là dữ liệu thô của client (vd <c>ScanResultDto</c>) để UI dựng banner Lưu/Thoát.</summary>
public sealed record TagCheckResult(bool Accepted, string? RejectionReason = null, object? Payload = null);

/// <summary>Dữ liệu lưu Scan theo kết quả máy (US-12 AC2).</summary>
public sealed record TesterScanSave(string TagCode, string BootId, int Seq, string GoldenCrc, string FirmwareVersion, DateTime TestedAt);

/// <summary>Kết quả lưu — <see cref="Accepted"/> = false nghĩa là server từ chối (vd tem bị chiếm giữa lúc kiểm tra và lúc lưu).</summary>
public sealed record TesterSaveResult(bool Accepted, string? RejectionReason = null, object? Payload = null);

/// <summary>Loại event log gửi lên server (khớp <c>TesterEventType</c> phía backend theo TÊN).</summary>
public enum TesterEventType
{
    VoidRemoved,
    VoidDiscard,
    VoidReboot,
    VoidLinkLost,
    LinkLost,
    LinkRestored,
    BindFail,
    ScanOnNg,
    DbError,
}

public sealed record TesterEvent(TesterEventType Type, string? TagCode = null, int? Seq = null, string? Detail = null, string? ApprovedBy = null);

/// <summary>
/// Các lời gọi server mà luồng máy thông điện cần. Mọi lỗi (mất mạng, 4xx/5xx) phải được ném ra dưới dạng exception với
/// <see cref="Exception.Message"/> là thông báo tiếng Việt sẵn để hiển thị (không có hàng đợi offline — US-12 AC14).
/// </summary>
public interface ITesterServerApi
{
    /// <summary>US-12 AC3/AC4: kiểm tra tem (FR-08), KHÔNG ghi dữ liệu.</summary>
    Task<TagCheckResult> CheckTagAsync(string tagCode, CancellationToken cancellationToken);

    /// <summary>US-12 AC2/AC15: lưu Scan OK theo kết quả máy — idempotent theo (BootId, Seq).</summary>
    Task<TesterSaveResult> SaveTesterScanAsync(TesterScanSave save, CancellationToken cancellationToken);

    /// <summary>US-14 AC12: ghi event log. Controller bắt mọi exception và chỉ ghi file khi lỗi.</summary>
    Task RecordEventAsync(TesterEvent testerEvent, CancellationToken cancellationToken);
}

/// <summary>Trạng thái App đối với máy thông điện (spec-app 7.1).</summary>
public enum TesterState
{
    /// <summary>Công tắc TẮT / trạm không có máy: không mở cổng, không PING.</summary>
    Stopped = 0,

    /// <summary>Chưa kết nối / mất kết nối / đang chờ đồng bộ — CHẶN quét.</summary>
    Disconnected,

    /// <summary>"Đặt dây và chờ thông điện".</summary>
    Ready,

    /// <summary>Đang giữ kết quả OK — "QUÉT MÃ QR TRÊN DÂY".</summary>
    WaitScan,

    /// <summary>Đã gửi BIND, chờ BIND_OK — "Đang xác nhận…".</summary>
    Binding,

    /// <summary>Đã BIND_OK, đang lưu lên server / chờ UNLOCKED — "Đang lưu…".</summary>
    Saving,

    /// <summary>Đã gửi DISCARD, chờ VOID.</summary>
    Discarding,
}

public enum TesterScanOutcomeKind
{
    /// <summary>Không nhận quét: mất kết nối / chưa bật.</summary>
    Blocked,

    /// <summary>Đang bận xử lý lệnh/quét khác — mã quét bị bỏ.</summary>
    Busy,

    /// <summary>Ở <c>Ready</c>: đã gửi <c>#BIND,SEQ=0</c>; kết quả hiển thị qua <see cref="TesterStationController.NoticeRaised"/>.</summary>
    SentToMachine,

    /// <summary>Ở <c>WaitScan</c>: tem bị FR-08 từ chối — KHÔNG gửi BIND; UI hiện banner Lưu/Thoát (<see cref="TesterScanOutcome.RejectionPayload"/>).</summary>
    Rejected,

    /// <summary>Không gọi được server kiểm tra tem — máy vẫn khóa, ở lại <c>WaitScan</c>.</summary>
    ServerError,

    /// <summary>Trạng thái đổi trong lúc chờ server (rút dây, mất kết nối...) — không gửi BIND.</summary>
    Aborted,

    /// <summary>Tem hợp lệ, đã gửi BIND; phần còn lại (BIND_OK -> lưu -> SAVED -> UNLOCKED) chạy bất đồng bộ.</summary>
    Bound,
}

public sealed record TesterScanOutcome(TesterScanOutcomeKind Kind, string Message = "", object? RejectionPayload = null);

public enum TesterNoticeKind
{
    /// <summary>"ĐÃ LƯU &lt;mã tem&gt;".</summary>
    Saved,

    /// <summary>"DÂY NG — dùng chế độ Scan NG" (US-12 AC7).</summary>
    WireNg,

    /// <summary>"Chưa thông điện" (AC8).</summary>
    NotTested,

    /// <summary>"Đang thông điện — chờ kết quả rồi quét lại" (AC8).</summary>
    Testing,

    /// <summary>"Dây đã bị rút — thông điện lại" (AC5/AC6).</summary>
    WireRemoved,

    /// <summary>Đã nhận lại kết quả OK từ máy (BAD_SEQ / đồng bộ) — quét lại QR (AC9).</summary>
    ResultRecovered,

    /// <summary>Máy reset làm mất kết quả đang chờ (US-14 AC5).</summary>
    MachineReset,

    /// <summary>Máy đã tự hủy kết quả vì mất link quá lâu (US-14 AC5).</summary>
    ResultVoidedByMachine,

    /// <summary>"Đang xác nhận lần trước — vui lòng chờ" (AC10).</summary>
    BindBusy,

    /// <summary>"Không phản hồi từ máy thông điện" (AC10).</summary>
    NoResponse,

    /// <summary>Máy báo lỗi / lý do chưa biết.</summary>
    MachineError,

    /// <summary>Lưu lên server lỗi — nút "Thử lại" (AC13).</summary>
    SaveFailed,

    /// <summary>Đã hủy kết quả theo yêu cầu của Tổ trưởng (AC11).</summary>
    Discarded,

    /// <summary>Mất kết nối máy (US-14 AC3).</summary>
    LinkLost,

    /// <summary>Kết nối máy có lại.</summary>
    LinkRestored,
}

public sealed record TesterNotice(TesterNoticeKind Kind, string Message, string? TagCode = null);

/// <summary>Văn bản hiển thị (tiếng Việt) dùng chung cho controller và UI.</summary>
public static class TesterTexts
{
    public const string Disconnected = "MẤT KẾT NỐI MÁY THÔNG ĐIỆN";
    public const string Ready = "Đặt dây và chờ thông điện";
    public const string WaitScan = "OK — QUÉT MÃ QR TRÊN DÂY";
    public const string Binding = "Đang xác nhận…";
    public const string Saving = "Đang lưu…";
    public const string SaveFailed = "LƯU LỖI — bấm Thử lại";
    public const string NotUnlocked = "CHƯA MỞ KHÓA MÁY — bấm Thử lại";
    public const string Discarding = "Đang hủy kết quả…";

    public const string ScanBlockedDisconnected = "Mất kết nối máy thông điện — không nhận mã quét.";
    public const string ScanBlockedStopped = "Máy thông điện đang không được sử dụng.";
    public const string ScanBusy = "Đang xử lý — vui lòng chờ.";
    public const string StateChanged = "Trạng thái máy đã thay đổi trong lúc kiểm tra tem — vui lòng quét lại.";

    public const string WireNg = "DÂY NG — dùng chế độ Scan NG";
    public const string NotTested = "Chưa thông điện";
    public const string Testing = "Đang thông điện — chờ kết quả rồi quét lại";
    public const string WireRemoved = "Dây đã bị rút — thông điện lại";
    public const string ResultRecovered = "Máy đang có kết quả OK — quét lại mã QR trên dây";
    public const string MachineReset = "Máy thông điện vừa khởi động lại — kết quả cũ đã mất, thông điện lại";
    public const string ResultVoidedByMachine = "Máy đã hủy kết quả đang chờ (mất kết nối quá lâu) — thông điện lại";
    public const string BindBusy = "Đang xác nhận lần trước — vui lòng chờ";
    public const string BindBusyFinal = "Máy thông điện đang bận — thử quét lại";
    public const string NoResponse = "Không phản hồi từ máy thông điện";
    public const string NotUnlockedNotice = "Đã lưu nhưng chưa nhận xác nhận mở khóa từ máy — bấm Thử lại";
    public const string Discarded = "Đã hủy kết quả — thông điện lại";
    public const string DiscardGone = "Kết quả đã không còn trên máy";
    public const string LinkLost = "Mất kết nối máy thông điện";
    public const string LinkRestored = "Đã kết nối lại máy thông điện";

    public static string Saved(string tagCode) => $"ĐÃ LƯU {tagCode}";

    public static string MachineError(string raw) => $"Máy thông điện báo lỗi: {raw}";
}
