using ProductionMES.Station.Tester.Protocol;

namespace ProductionMES.Station.Tester.Tests;

/// <summary>US-14 AC10 — parser chịu lỗi: key đảo thứ tự, key lạ, thiếu key, dòng không '#', \r\n, dòng &gt; 256 byte.</summary>
public class ProtocolParserTests
{
    [Fact]
    public void Parse_Hello_DayDu()
    {
        var result = ProtocolParser.Parse("#HELLO,FW=1.4.0,PINS=16,GM=3A7F,BOOT=8F21C04D");

        Assert.Equal(ParseStatus.Ok, result.Status);
        var hello = Assert.IsType<HelloMessage>(result.Message);
        Assert.Equal("1.4.0", hello.Firmware);
        Assert.Equal(16, hello.Pins);
        Assert.Equal("3A7F", hello.GoldenCrc);
        Assert.Equal("8F21C04D", hello.BootId);
    }

    // Tra key theo tên, không theo vị trí.
    [Fact]
    public void Parse_KeyDaoThuTu_VanDung()
    {
        var result = ProtocolParser.Parse("#HELLO,BOOT=8F21C04D,GM=0000,PINS=64,FW=1.0.0");

        var hello = Assert.IsType<HelloMessage>(result.Message);
        Assert.Equal("1.0.0", hello.Firmware);
        Assert.Equal(64, hello.Pins);
        Assert.Equal("0000", hello.GoldenCrc);
        Assert.Equal("8F21C04D", hello.BootId);
    }

    // Key lạ bị bỏ qua (firmware thêm trường sau này không làm App lỗi).
    [Fact]
    public void Parse_KeyLa_BoQua()
    {
        var result = ProtocolParser.Parse("#PONG,FOO=bar,ST=WAIT_BIND,SEQ=105,EXTRA=1");

        var pong = Assert.IsType<PongMessage>(result.Message);
        Assert.Equal(MachineState.WaitBind, pong.State);
        Assert.Equal(105, pong.Seq);
    }

    [Theory]
    [InlineData("#HELLO,FW=1.0.0,PINS=16,GM=3A7F")]
    [InlineData("#HELLO,PINS=16,GM=3A7F,BOOT=1")]
    [InlineData("#PONG,ST=IDLE")]
    [InlineData("#PONG,SEQ=1")]
    [InlineData("#RESULT,SEQ=1,R=OK")]
    [InlineData("#RESULT,R=OK,GM=3A7F")]
    [InlineData("#BIND_OK")]
    [InlineData("#BIND_FAIL,SEQ=1")]
    [InlineData("#UNLOCKED")]
    [InlineData("#VOID,SEQ=1")]
    [InlineData("#ERR")]
    public void Parse_ThieuKeyBatBuoc_LaDongHong(string line)
    {
        var result = ProtocolParser.Parse(line);

        Assert.Equal(ParseStatus.Invalid, result.Status);
        Assert.Null(result.Message);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Coverage] pin 3 ok")]
    [InlineData("ets Jun  8 2016 00:22:57")]
    [InlineData("  #PING")]
    public void Parse_DongKhongBatDauBangThang_KhongPhaiGiaoThuc(string line)
    {
        Assert.Equal(ParseStatus.NotProtocol, ProtocolParser.Parse(line).Status);
    }

    [Fact]
    public void Parse_Null_KhongPhaiGiaoThuc()
    {
        Assert.Equal(ParseStatus.NotProtocol, ProtocolParser.Parse(null).Status);
    }

    // \r\n: chấp nhận CRLF.
    [Fact]
    public void Parse_KetThucCrLf_VanDung()
    {
        var result = ProtocolParser.Parse("#UNLOCKED,SEQ=7\r\n");

        Assert.Equal(7, Assert.IsType<UnlockedMessage>(result.Message).Seq);
    }

    [Fact]
    public void Parse_DongQua256Byte_LaDongHong()
    {
        var line = "#HELLO,FW=" + new string('x', 300);

        Assert.Equal(ParseStatus.Invalid, ProtocolParser.Parse(line).Status);
    }

    [Fact]
    public void Parse_DongDung256ByteKemXuongDong_ChapNhan()
    {
        // 255 ký tự + '\n' = 256 byte: hợp lệ về độ dài (key lạ dài, bị bỏ qua).
        var prefix = "#BIND_OK,SEQ=1,PAD=";
        var line = prefix + new string('x', 255 - prefix.Length);
        Assert.Equal(255, line.Length);

        Assert.Equal(ParseStatus.Ok, ProtocolParser.Parse(line).Status);
    }

    [Fact]
    public void Parse_LenhLa_LaDongHong()
    {
        Assert.Equal(ParseStatus.Invalid, ProtocolParser.Parse("#WHAT,SEQ=1").Status);
    }

    [Fact]
    public void Parse_TruongKhongCoDauBang_LaDongHong()
    {
        Assert.Equal(ParseStatus.Invalid, ProtocolParser.Parse("#PONG,ST=IDLE,SEQ").Status);
    }

    [Theory]
    [InlineData("#PONG,ST=IDLE,SEQ=-1")]
    [InlineData("#PONG,ST=IDLE,SEQ=abc")]
    [InlineData("#PONG,ST=IDLE,SEQ=99999999999")]
    public void Parse_SeqKhongHopLe_LaDongHong(string line)
    {
        Assert.Equal(ParseStatus.Invalid, ProtocolParser.Parse(line).Status);
    }

    [Fact]
    public void Parse_Result_DayDu()
    {
        var result = ProtocolParser.Parse("#RESULT,SEQ=105,R=OK,GM=3A7F");

        var message = Assert.IsType<ResultMessage>(result.Message);
        Assert.Equal(105, message.Seq);
        Assert.Equal("OK", message.Result);
        Assert.Equal("3A7F", message.GoldenCrc);
    }

    [Theory]
    [InlineData("NG", BindFailReason.Ng)]
    [InlineData("TESTING", BindFailReason.Testing)]
    [InlineData("NO_PENDING", BindFailReason.NoPending)]
    [InlineData("BAD_SEQ", BindFailReason.BadSeq)]
    [InlineData("BUSY", BindFailReason.Busy)]
    [InlineData("REMOVED", BindFailReason.Removed)]
    [InlineData("SOMETHING_NEW", BindFailReason.Unknown)]
    public void Parse_BindFail_MoiLyDo(string reason, BindFailReason expected)
    {
        var result = ProtocolParser.Parse($"#BIND_FAIL,SEQ=0,REASON={reason}");

        var message = Assert.IsType<BindFailMessage>(result.Message);
        Assert.Equal(expected, message.Reason);
        Assert.Equal(0, message.Seq);
    }

    [Fact]
    public void Parse_BindFailKhongCoSeq_VanDung()
    {
        var message = Assert.IsType<BindFailMessage>(ProtocolParser.Parse("#BIND_FAIL,REASON=BUSY").Message);

        Assert.Null(message.Seq);
        Assert.Equal(BindFailReason.Busy, message.Reason);
    }

    [Theory]
    [InlineData("REMOVED", VoidReason.Removed)]
    [InlineData("DISCARD", VoidReason.Discard)]
    [InlineData("X", VoidReason.Unknown)]
    public void Parse_Void(string reason, VoidReason expected)
    {
        var message = Assert.IsType<VoidMessage>(ProtocolParser.Parse($"#VOID,SEQ=106,REASON={reason}").Message);

        Assert.Equal(expected, message.Reason);
        Assert.Equal(106, message.Seq);
    }

    [Theory]
    [InlineData("BAD_CMD", ErrorCode.BadCmd)]
    [InlineData("BAD_SEQ", ErrorCode.BadSeq)]
    [InlineData("NO_PENDING", ErrorCode.NoPending)]
    [InlineData("TOO_LONG", ErrorCode.TooLong)]
    [InlineData("BUSY", ErrorCode.Busy)]
    [InlineData("OTHER", ErrorCode.Unknown)]
    public void Parse_Err(string code, ErrorCode expected)
    {
        var message = Assert.IsType<ErrorMessage>(ProtocolParser.Parse($"#ERR,CODE={code}").Message);

        Assert.Equal(expected, message.Code);
    }

    [Theory]
    [InlineData("IDLE", MachineState.Idle)]
    [InlineData("TESTING", MachineState.Testing)]
    [InlineData("WAIT_BIND", MachineState.WaitBind)]
    [InlineData("BINDING", MachineState.Binding)]
    [InlineData("WAIT_REMOVE", MachineState.WaitRemove)]
    [InlineData("BROWSE", MachineState.Browse)]
    [InlineData("NO_SAMPLE", MachineState.NoSample)]
    [InlineData("FUTURE", MachineState.Unknown)]
    public void Parse_Pong_MoiTrangThai(string st, MachineState expected)
    {
        var message = Assert.IsType<PongMessage>(ProtocolParser.Parse($"#PONG,ST={st},SEQ=0").Message);

        Assert.Equal(expected, message.State);
    }

    [Fact]
    public void Parse_BindOk()
    {
        Assert.Equal(105, Assert.IsType<BindOkMessage>(ProtocolParser.Parse("#BIND_OK,SEQ=105").Message).Seq);
    }

    [Theory]
    [InlineData("#PING")]
    [InlineData("#BIND,SEQ=1")]
    public void Parse_LenhCuaAppGuiXuongMay_KhongPhaiTinNhanTuMay(string line)
    {
        Assert.Equal(ParseStatus.Invalid, ProtocolParser.Parse(line).Status);
    }

    [Fact]
    public void Command_ToLine_DungDinhDang()
    {
        Assert.Equal("#PING", TesterCommand.Ping().ToLine());
        Assert.Equal("#BIND,SEQ=0", TesterCommand.Bind(0).ToLine());
        Assert.Equal("#BIND,SEQ=105", TesterCommand.Bind(105).ToLine());
        Assert.Equal("#SAVED,SEQ=105", TesterCommand.Saved(105).ToLine());
        Assert.Equal("#DISCARD,SEQ=105", TesterCommand.Discard(105).ToLine());
        Assert.Equal("#RESULT?", TesterCommand.ResultQuery().ToLine());
        Assert.Equal("#HELLO?", TesterCommand.HelloQuery().ToLine());
    }
}
