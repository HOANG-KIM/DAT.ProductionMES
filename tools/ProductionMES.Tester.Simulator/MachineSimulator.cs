namespace ProductionMES.Tester.Simulator;

/// <summary>
/// Giả lập firmware máy thông điện ESP32 theo `Documents/spec-firmware-app-link.md` mục 4-5 (chỉ phần giao thức serial,
/// không mô phỏng quét pin). Logic thuần (không IO) để dùng cả cho công cụ console (<c>Program</c>, nối cổng COM ảo
/// com0com) lẫn test tích hợp của <c>Station.Tester.Tests</c>.
/// </summary>
/// <remarks>
/// Bảng xử lý <c>#BIND</c> (mục 5.3, theo đúng thứ tự): (1) đang Live scan -> TESTING; (2) kBindWait nhưng SEQ khác ->
/// BAD_SEQ; (3) kWaitSaved -> BUSY; (4) lastFail -> NG; (5) còn lại -> NO_PENDING. Mất link quá 5 giây khi đang khóa ->
/// hủy tại chỗ (không gửi được VOID).
/// </remarks>
public sealed class MachineSimulator
{
    /// <summary>Quá thời gian này không có PING thì coi là mất link (D3).</summary>
    public const int LinkTimeoutMs = 5000;

    private enum St
    {
        Idle,
        Testing,
        WaitBind,
        WaitSaved,
        WaitRemove,
    }

    private readonly Func<long> _nowMs;
    private readonly object _lock = new();
    private St _st = St.Idle;
    private int _seq;
    private int _pendingSeq;
    private bool _lastFail;
    private long _lastPingMs = long.MinValue / 2;
    private string _lastResultLine = string.Empty;
    private int _bootCounter;

    public MachineSimulator(Func<long>? nowMs = null, string? bootId = null)
    {
        _nowMs = nowMs ?? (() => Environment.TickCount64);
        BootId = bootId ?? NextBootId();
    }

    /// <summary>Máy "gửi" 1 dòng cho App (không gồm '\n'). Được gọi trong lock — người nhận chỉ nên xếp hàng.</summary>
    public event Action<string>? Send;

    public string FirmwareVersion { get; set; } = "1.0.0";

    public int Pins { get; set; } = 16;

    /// <summary>GM — "0000" = chưa có mẫu.</summary>
    public string GoldenCrc { get; set; } = "3A7F";

    public string BootId { get; private set; }

    /// <summary>false = máy vẫn nhận lệnh nhưng không trả PONG (giả lập treo).</summary>
    public bool RespondToPing { get; set; } = true;

    /// <summary>Trạng thái hiện tại theo tên <c>ST</c> của PONG.</summary>
    public string StateName
    {
        get
        {
            lock (_lock)
            {
                return StateText();
            }
        }
    }

    public int PendingSeq
    {
        get
        {
            lock (_lock)
            {
                return PongSeq();
            }
        }
    }

    public bool IsAppConnected
    {
        get
        {
            lock (_lock)
            {
                return AppConnected();
            }
        }
    }

    /// <summary>Máy vừa bật nguồn / reset (nút EN): SEQ về 0, BOOT mới, gửi <c>#HELLO</c>.</summary>
    public void Boot(string? newBootId = null)
    {
        lock (_lock)
        {
            BootId = newBootId ?? NextBootId();
            _st = St.Idle;
            _seq = 0;
            _pendingSeq = 0;
            _lastFail = false;
            _lastPingMs = long.MinValue / 2;
            EmitHello();
        }
    }

    /// <summary>Đặt dây lên jig -> bắt đầu thông điện (Idle -> Testing).</summary>
    public void InsertWire()
    {
        lock (_lock)
        {
            if (_st == St.Idle)
            {
                _st = St.Testing;
                _lastFail = false;
            }
        }
    }

    /// <summary>Kết thúc thông điện (Testing -> kết quả). OK + có App: khóa chờ quét QR + gửi RESULT; NG: không gửi gì, không khóa.</summary>
    public void FinishTest(bool pass)
    {
        lock (_lock)
        {
            if (_st != St.Testing)
            {
                return;
            }

            if (pass)
            {
                if (AppConnected())
                {
                    _seq++;
                    _pendingSeq = _seq;
                    _st = St.WaitBind;
                    _lastResultLine = $"#RESULT,SEQ={_seq},R=OK,GM={GoldenCrc}";
                    Emit(_lastResultLine);
                }
                else
                {
                    _st = St.WaitRemove;
                }
            }
            else
            {
                _st = St.WaitRemove;
                _lastFail = true;
            }
        }
    }

    /// <summary>Rút dây ra khỏi jig. Đang khóa (WaitBind/WaitSaved) -> gửi <c>#VOID,REASON=REMOVED</c>.</summary>
    public void RemoveWire()
    {
        lock (_lock)
        {
            if (_st is St.WaitBind or St.WaitSaved)
            {
                Emit($"#VOID,SEQ={_pendingSeq},REASON=REMOVED");
            }

            _st = St.Idle;
            _pendingSeq = 0;
            _lastFail = false;
        }
    }

    /// <summary>Gọi định kỳ: phát hiện mất link khi đang khóa -> hủy tại chỗ.</summary>
    public void Tick()
    {
        lock (_lock)
        {
            if (_st is St.WaitBind or St.WaitSaved && !AppConnected())
            {
                _st = St.WaitRemove;
                _pendingSeq = 0;
            }
        }
    }

    /// <summary>Xử lý 1 dòng nhận từ App.</summary>
    public void HandleLine(string line)
    {
        line = line.TrimEnd('\r', '\n');
        if (line.Length == 0 || line[0] != '#')
        {
            return;
        }

        var tokens = line[1..].Split(',');
        var command = tokens[0];
        var seq = 0;
        foreach (var token in tokens.Skip(1))
        {
            var eq = token.IndexOf('=');
            if (eq > 0 && token[..eq] == "SEQ")
            {
                int.TryParse(token[(eq + 1)..], out seq);
            }
        }

        lock (_lock)
        {
            switch (command)
            {
                case "PING":
                    OnPing();
                    break;
                case "BIND":
                    OnBind(seq);
                    break;
                case "SAVED":
                    OnSaved(seq);
                    break;
                case "DISCARD":
                    OnDiscard(seq);
                    break;
                case "RESULT?":
                    if (_st is St.WaitBind or St.WaitSaved)
                    {
                        Emit(_lastResultLine);
                    }
                    else
                    {
                        Emit("#ERR,CODE=NO_PENDING");
                    }

                    break;
                case "HELLO?":
                    EmitHello();
                    break;
                default:
                    Emit("#ERR,CODE=BAD_CMD");
                    break;
            }
        }
    }

    private void OnPing()
    {
        var wasUp = AppConnected();
        _lastPingMs = _nowMs();
        if (!wasUp)
        {
            // Link chuyển từ mất -> có: gửi HELLO (mục 4.3).
            EmitHello();
        }

        if (RespondToPing)
        {
            Emit($"#PONG,ST={StateText()},SEQ={PongSeq()}");
        }
    }

    private void OnBind(int seq)
    {
        if (_st == St.WaitBind && seq == _pendingSeq)
        {
            _st = St.WaitSaved;
            Emit($"#BIND_OK,SEQ={seq}");
            return;
        }

        if (_st == St.Testing)
        {
            Emit($"#BIND_FAIL,SEQ={seq},REASON=TESTING");
        }
        else if (_st == St.WaitBind)
        {
            Emit($"#BIND_FAIL,SEQ={seq},REASON=BAD_SEQ");
        }
        else if (_st == St.WaitSaved)
        {
            Emit($"#BIND_FAIL,SEQ={seq},REASON=BUSY");
        }
        else if (_lastFail)
        {
            Emit($"#BIND_FAIL,SEQ={seq},REASON=NG");
        }
        else
        {
            Emit($"#BIND_FAIL,SEQ={seq},REASON=NO_PENDING");
        }
    }

    private void OnSaved(int seq)
    {
        if (_st == St.WaitSaved && seq == _pendingSeq)
        {
            Emit($"#UNLOCKED,SEQ={seq}");
            _st = St.WaitRemove;
            _pendingSeq = 0;
            return;
        }

        Emit(_st is St.WaitBind or St.WaitSaved ? "#ERR,CODE=BAD_SEQ" : "#ERR,CODE=NO_PENDING");
    }

    private void OnDiscard(int seq)
    {
        if (_st is St.WaitBind or St.WaitSaved && seq == _pendingSeq)
        {
            Emit($"#VOID,SEQ={seq},REASON=DISCARD");
            _st = St.WaitRemove;
            _pendingSeq = 0;
            return;
        }

        Emit(_st is St.WaitBind or St.WaitSaved ? "#ERR,CODE=BAD_SEQ" : "#ERR,CODE=NO_PENDING");
    }

    private bool AppConnected() => _nowMs() - _lastPingMs < LinkTimeoutMs;

    private int PongSeq() => _st is St.WaitBind or St.WaitSaved ? _pendingSeq : 0;

    private string StateText()
    {
        if (GoldenCrc == "0000" && _st == St.Idle)
        {
            return "NO_SAMPLE";
        }

        return _st switch
        {
            St.Idle => "IDLE",
            St.Testing => "TESTING",
            St.WaitBind => "WAIT_BIND",
            St.WaitSaved => "BINDING",
            St.WaitRemove => "WAIT_REMOVE",
            _ => "IDLE",
        };
    }

    private void EmitHello() => Emit($"#HELLO,FW={FirmwareVersion},PINS={Pins},GM={GoldenCrc},BOOT={BootId}");

    private void Emit(string line) => Send?.Invoke(line);

    private string NextBootId()
    {
        _bootCounter++;
        return (0x8F21C04Du + (uint)_bootCounter * 0x01010101u).ToString("X8");
    }
}
