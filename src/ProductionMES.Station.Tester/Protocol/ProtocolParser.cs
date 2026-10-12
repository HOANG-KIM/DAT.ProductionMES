using System.Globalization;

namespace ProductionMES.Station.Tester.Protocol;

/// <summary>Kết quả parse 1 dòng nhận từ cổng COM.</summary>
public enum ParseStatus
{
    /// <summary>Dòng giao thức hợp lệ, xem <see cref="ParseResult.Message"/>.</summary>
    Ok,

    /// <summary>Không bắt đầu bằng <c>#</c> — log debug của firmware / rác bootloader: chỉ ghi file, không xử lý (US-14 AC10).</summary>
    NotProtocol,

    /// <summary>Bắt đầu bằng <c>#</c> nhưng hỏng: lệnh lạ, thiếu key bắt buộc, giá trị sai, quá dài — ghi log và bỏ.</summary>
    Invalid,
}

public readonly record struct ParseResult(ParseStatus Status, TesterMessage? Message, string? Error)
{
    public static ParseResult Success(TesterMessage message) => new(ParseStatus.Ok, message, null);

    public static ParseResult NotProtocol() => new(ParseStatus.NotProtocol, null, null);

    public static ParseResult Invalid(string error) => new(ParseStatus.Invalid, null, error);
}

/// <summary>
/// Parse dòng giao thức máy -> App (spec-firmware-app-link.md mục 4, US-14 AC10). Tra key theo TÊN (không theo vị trí),
/// bỏ qua key lạ (firmware thêm trường sau này không làm App lỗi), coi dòng thiếu key bắt buộc là hỏng. Không bao giờ
/// ném exception.
/// </summary>
public static class ProtocolParser
{
    /// <summary>Độ dài tối đa 1 dòng kể cả <c>\n</c> (mục 4.1).</summary>
    public const int MaxLineLength = 256;

    public static ParseResult Parse(string? line)
    {
        if (line is null)
        {
            return ParseResult.NotProtocol();
        }

        // Chấp nhận \r\n: cắt \r\n cuối dòng nếu còn sót.
        line = line.TrimEnd('\r', '\n');

        if (line.Length == 0 || line[0] != '#')
        {
            return ParseResult.NotProtocol();
        }

        // +1 cho '\n' đã bị cắt.
        if (line.Length + 1 > MaxLineLength)
        {
            return ParseResult.Invalid($"Dòng quá dài ({line.Length + 1} byte > {MaxLineLength}).");
        }

        var tokens = line[1..].Split(',');
        var command = tokens[0].Trim().ToUpperInvariant();
        var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < tokens.Length; i++)
        {
            var token = tokens[i];
            var eq = token.IndexOf('=');
            if (eq <= 0)
            {
                return ParseResult.Invalid($"Trường không có dạng KEY=VALUE: '{token}'.");
            }

            keys[token[..eq].Trim()] = token[(eq + 1)..].Trim();
        }

        try
        {
            return command switch
            {
                "HELLO" => ParseHello(keys),
                "PONG" => ParsePong(keys),
                "RESULT" => ParseResultMessage(keys),
                "BIND_OK" => TryGetSeq(keys, out var bindOkSeq, out var bindOkErr)
                    ? ParseResult.Success(new BindOkMessage(bindOkSeq))
                    : ParseResult.Invalid(bindOkErr),
                "BIND_FAIL" => ParseBindFail(keys),
                "UNLOCKED" => TryGetSeq(keys, out var unlockedSeq, out var unlockedErr)
                    ? ParseResult.Success(new UnlockedMessage(unlockedSeq))
                    : ParseResult.Invalid(unlockedErr),
                "VOID" => ParseVoid(keys),
                "ERR" => ParseError(keys),
                _ => ParseResult.Invalid($"Lệnh không biết: '{command}'."),
            };
        }
        catch (Exception ex)
        {
            // Phòng vệ cuối cùng: parser không được làm sập App.
            return ParseResult.Invalid($"Lỗi parse: {ex.Message}");
        }
    }

    private static ParseResult ParseHello(Dictionary<string, string> keys)
    {
        if (!keys.TryGetValue("FW", out var fw) || fw.Length == 0)
        {
            return ParseResult.Invalid("HELLO thiếu FW.");
        }

        if (!keys.TryGetValue("PINS", out var pinsText) || !int.TryParse(pinsText, NumberStyles.None, CultureInfo.InvariantCulture, out var pins))
        {
            return ParseResult.Invalid("HELLO thiếu hoặc sai PINS.");
        }

        if (!keys.TryGetValue("GM", out var gm) || gm.Length == 0)
        {
            return ParseResult.Invalid("HELLO thiếu GM.");
        }

        if (!keys.TryGetValue("BOOT", out var boot) || boot.Length == 0)
        {
            return ParseResult.Invalid("HELLO thiếu BOOT.");
        }

        return ParseResult.Success(new HelloMessage(fw, pins, gm.ToUpperInvariant(), boot.ToUpperInvariant()));
    }

    private static ParseResult ParsePong(Dictionary<string, string> keys)
    {
        if (!keys.TryGetValue("ST", out var st) || st.Length == 0)
        {
            return ParseResult.Invalid("PONG thiếu ST.");
        }

        if (!TryGetSeq(keys, out var seq, out var error))
        {
            return ParseResult.Invalid("PONG: " + error);
        }

        return ParseResult.Success(new PongMessage(ParseState(st), seq, st.ToUpperInvariant()));
    }

    private static ParseResult ParseResultMessage(Dictionary<string, string> keys)
    {
        if (!TryGetSeq(keys, out var seq, out var error))
        {
            return ParseResult.Invalid("RESULT: " + error);
        }

        if (!keys.TryGetValue("R", out var r) || r.Length == 0)
        {
            return ParseResult.Invalid("RESULT thiếu R.");
        }

        if (!keys.TryGetValue("GM", out var gm) || gm.Length == 0)
        {
            return ParseResult.Invalid("RESULT thiếu GM.");
        }

        return ParseResult.Success(new ResultMessage(seq, r.ToUpperInvariant(), gm.ToUpperInvariant()));
    }

    private static ParseResult ParseBindFail(Dictionary<string, string> keys)
    {
        if (!keys.TryGetValue("REASON", out var reason) || reason.Length == 0)
        {
            return ParseResult.Invalid("BIND_FAIL thiếu REASON.");
        }

        var seq = TryGetOptionalSeq(keys);
        return ParseResult.Success(new BindFailMessage(seq, ParseBindFailReason(reason), reason.ToUpperInvariant()));
    }

    private static ParseResult ParseVoid(Dictionary<string, string> keys)
    {
        if (!keys.TryGetValue("REASON", out var reason) || reason.Length == 0)
        {
            return ParseResult.Invalid("VOID thiếu REASON.");
        }

        var seq = TryGetOptionalSeq(keys);
        return ParseResult.Success(new VoidMessage(seq, ParseVoidReason(reason), reason.ToUpperInvariant()));
    }

    private static ParseResult ParseError(Dictionary<string, string> keys)
    {
        if (!keys.TryGetValue("CODE", out var code) || code.Length == 0)
        {
            return ParseResult.Invalid("ERR thiếu CODE.");
        }

        return ParseResult.Success(new ErrorMessage(ParseErrorCode(code), code.ToUpperInvariant()));
    }

    private static bool TryGetSeq(Dictionary<string, string> keys, out int seq, out string error)
    {
        seq = 0;
        error = string.Empty;
        if (!keys.TryGetValue("SEQ", out var text))
        {
            error = "thiếu SEQ.";
            return false;
        }

        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out seq))
        {
            error = $"SEQ không hợp lệ: '{text}'.";
            return false;
        }

        return true;
    }

    private static int? TryGetOptionalSeq(Dictionary<string, string> keys)
        => keys.TryGetValue("SEQ", out var text) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var seq)
            ? seq
            : null;

    private static MachineState ParseState(string st) => st.ToUpperInvariant() switch
    {
        "IDLE" => MachineState.Idle,
        "TESTING" => MachineState.Testing,
        "WAIT_BIND" => MachineState.WaitBind,
        "BINDING" => MachineState.Binding,
        "WAIT_REMOVE" => MachineState.WaitRemove,
        "BROWSE" => MachineState.Browse,
        "NO_SAMPLE" => MachineState.NoSample,
        _ => MachineState.Unknown,
    };

    private static BindFailReason ParseBindFailReason(string reason) => reason.ToUpperInvariant() switch
    {
        "NG" => BindFailReason.Ng,
        "TESTING" => BindFailReason.Testing,
        "NO_PENDING" => BindFailReason.NoPending,
        "BAD_SEQ" => BindFailReason.BadSeq,
        "BUSY" => BindFailReason.Busy,
        "REMOVED" => BindFailReason.Removed,
        _ => BindFailReason.Unknown,
    };

    private static VoidReason ParseVoidReason(string reason) => reason.ToUpperInvariant() switch
    {
        "REMOVED" => VoidReason.Removed,
        "DISCARD" => VoidReason.Discard,
        _ => VoidReason.Unknown,
    };

    private static ErrorCode ParseErrorCode(string code) => code.ToUpperInvariant() switch
    {
        "BAD_CMD" => ErrorCode.BadCmd,
        "BAD_SEQ" => ErrorCode.BadSeq,
        "NO_PENDING" => ErrorCode.NoPending,
        "TOO_LONG" => ErrorCode.TooLong,
        "BUSY" => ErrorCode.Busy,
        _ => ErrorCode.Unknown,
    };
}
