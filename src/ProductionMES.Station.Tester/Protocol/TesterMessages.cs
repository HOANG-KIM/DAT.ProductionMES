namespace ProductionMES.Station.Tester.Protocol;

/// <summary>Trạng thái máy thông điện trong <c>#PONG,ST=...</c> (spec-firmware-app-link.md mục 4.3).</summary>
public enum MachineState
{
    Unknown = 0,
    Idle,
    Testing,
    WaitBind,
    Binding,
    WaitRemove,
    Browse,
    NoSample,
}

/// <summary><c>REASON</c> của <c>#BIND_FAIL</c>. <see cref="Removed"/> không có trong firmware hiện tại nhưng vẫn được xử lý phòng khi firmware bổ sung.</summary>
public enum BindFailReason
{
    Unknown = 0,
    Ng,
    Testing,
    NoPending,
    BadSeq,
    Busy,
    Removed,
}

/// <summary><c>REASON</c> của <c>#VOID</c>.</summary>
public enum VoidReason
{
    Unknown = 0,
    Removed,
    Discard,
}

/// <summary><c>CODE</c> của <c>#ERR</c>.</summary>
public enum ErrorCode
{
    Unknown = 0,
    BadCmd,
    BadSeq,
    NoPending,
    TooLong,
    Busy,
}

/// <summary>Dòng giao thức máy -> App đã parse (mục 4.3 spec-firmware-app-link.md).</summary>
public abstract record TesterMessage;

/// <summary><c>#HELLO,FW=..,PINS=..,GM=..,BOOT=..</c> — lúc boot, khi link có lại, hoặc trả lời <c>#HELLO?</c>.</summary>
public sealed record HelloMessage(string Firmware, int Pins, string GoldenCrc, string BootId) : TesterMessage;

/// <summary><c>#PONG,ST=..,SEQ=..</c> — <c>Seq</c> là SEQ đang chờ, 0 nếu không có.</summary>
public sealed record PongMessage(MachineState State, int Seq, string RawState) : TesterMessage;

/// <summary><c>#RESULT,SEQ=n,R=OK,GM=..</c> — chỉ gửi khi thông điện OK.</summary>
public sealed record ResultMessage(int Seq, string Result, string GoldenCrc) : TesterMessage;

/// <summary><c>#BIND_OK,SEQ=n</c>.</summary>
public sealed record BindOkMessage(int Seq) : TesterMessage;

/// <summary><c>#BIND_FAIL[,SEQ=n],REASON=..</c>.</summary>
public sealed record BindFailMessage(int? Seq, BindFailReason Reason, string RawReason) : TesterMessage;

/// <summary><c>#UNLOCKED,SEQ=n</c>.</summary>
public sealed record UnlockedMessage(int Seq) : TesterMessage;

/// <summary><c>#VOID[,SEQ=n],REASON=..</c>.</summary>
public sealed record VoidMessage(int? Seq, VoidReason Reason, string RawReason) : TesterMessage;

/// <summary><c>#ERR,CODE=..</c>.</summary>
public sealed record ErrorMessage(ErrorCode Code, string RawCode) : TesterMessage;

/// <summary>Loại lệnh App -> máy.</summary>
public enum TesterCommandKind
{
    Ping,
    Bind,
    Saved,
    Discard,
    ResultQuery,
    HelloQuery,
}

/// <summary>Lệnh App -> máy (mục 4.2). <see cref="Seq"/> chỉ có nghĩa với Bind/Saved/Discard.</summary>
public readonly record struct TesterCommand(TesterCommandKind Kind, int Seq = 0)
{
    public static TesterCommand Ping() => new(TesterCommandKind.Ping);

    public static TesterCommand Bind(int seq) => new(TesterCommandKind.Bind, seq);

    public static TesterCommand Saved(int seq) => new(TesterCommandKind.Saved, seq);

    public static TesterCommand Discard(int seq) => new(TesterCommandKind.Discard, seq);

    public static TesterCommand ResultQuery() => new(TesterCommandKind.ResultQuery);

    public static TesterCommand HelloQuery() => new(TesterCommandKind.HelloQuery);

    /// <summary>Dòng gửi xuống máy, KHÔNG gồm ký tự xuống dòng.</summary>
    public string ToLine() => Kind switch
    {
        TesterCommandKind.Ping => "#PING",
        TesterCommandKind.Bind => $"#BIND,SEQ={Seq}",
        TesterCommandKind.Saved => $"#SAVED,SEQ={Seq}",
        TesterCommandKind.Discard => $"#DISCARD,SEQ={Seq}",
        TesterCommandKind.ResultQuery => "#RESULT?",
        TesterCommandKind.HelloQuery => "#HELLO?",
        _ => throw new InvalidOperationException($"Lệnh không hợp lệ: {Kind}"),
    };
}
