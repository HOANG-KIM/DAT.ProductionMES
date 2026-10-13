using System.Text;
using ProductionMES.Station.Tester.Protocol;

namespace ProductionMES.Station.Tester.Tests;

/// <summary>US-14 AC10 — ghép dòng từ luồng byte: \r\n, dòng bị cắt giữa chừng, dòng &gt; 256 byte.</summary>
public class LineAssemblerTests
{
    private static List<string> Feed(LineAssembler assembler, string text, out int overflows)
    {
        var lines = new List<string>();
        var count = 0;
        var bytes = Encoding.ASCII.GetBytes(text);
        assembler.Feed(bytes, 0, bytes.Length, lines.Add, () => count++);
        overflows = count;
        return lines;
    }

    [Fact]
    public void Feed_CatCrCuoiDong()
    {
        var lines = Feed(new LineAssembler(), "#PING\r\n#PONG,ST=IDLE,SEQ=0\n", out _);

        Assert.Equal(new[] { "#PING", "#PONG,ST=IDLE,SEQ=0" }, lines);
    }

    [Fact]
    public void Feed_DongBiCatNhieuLan_GhepDung()
    {
        var assembler = new LineAssembler();

        var first = Feed(assembler, "#PONG,ST=ID", out _);
        var second = Feed(assembler, "LE,SEQ=0\r", out _);
        var third = Feed(assembler, "\n", out _);

        Assert.Empty(first);
        Assert.Empty(second);
        Assert.Equal(new[] { "#PONG,ST=IDLE,SEQ=0" }, third);
    }

    [Fact]
    public void Feed_DongLogKhongBatDauBangThang_VanDuocDuaRaDeTangTrenBoQua()
    {
        var lines = Feed(new LineAssembler(), "[Coverage] ok\n#PONG,ST=IDLE,SEQ=0\n", out _);

        Assert.Equal(2, lines.Count);
        Assert.Equal("[Coverage] ok", lines[0]);
    }

    [Fact]
    public void Feed_DongQua256Byte_BoCaDongRoiDongBoLaiDungDong()
    {
        var longLine = "#HELLO,FW=" + new string('x', 400) + "\n";

        var lines = Feed(new LineAssembler(), longLine + "#UNLOCKED,SEQ=3\n", out var overflows);

        Assert.Equal(1, overflows);
        Assert.Equal(new[] { "#UNLOCKED,SEQ=3" }, lines);
    }

    [Fact]
    public void Feed_DongDung255KyTuCongXuongDong_KhongBiBo()
    {
        var content = "#" + new string('a', 254);
        Assert.Equal(255, content.Length);

        var lines = Feed(new LineAssembler(), content + "\n", out var overflows);

        Assert.Equal(0, overflows);
        Assert.Equal(new[] { content }, lines);
    }

    [Fact]
    public void Feed_DongDai256KyTuChuaXuongDong_BiBo()
    {
        var content = "#" + new string('a', 255);
        Assert.Equal(256, content.Length);

        var lines = Feed(new LineAssembler(), content + "\n", out var overflows);

        Assert.Equal(1, overflows);
        Assert.Empty(lines);
    }

    [Fact]
    public void Feed_ByteRacBootloader_KhongLamSap()
    {
        var assembler = new LineAssembler();
        var garbage = new byte[] { 0xFF, 0xFE, 0x00, 0x80, (byte)'\n' };
        var lines = new List<string>();

        assembler.Feed(garbage, 0, garbage.Length, lines.Add);

        Assert.Single(lines);
        Assert.Equal(ParseStatus.NotProtocol, ProtocolParser.Parse(lines[0]).Status);
    }

    [Fact]
    public void Reset_BoDuLieuDangGhepDo()
    {
        var assembler = new LineAssembler();
        Feed(assembler, "#PONG,ST=ID", out _);

        assembler.Reset();
        var lines = Feed(assembler, "#UNLOCKED,SEQ=1\n", out _);

        Assert.Equal(new[] { "#UNLOCKED,SEQ=1" }, lines);
    }
}
