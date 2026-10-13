using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Validators.Scans;

namespace ProductionMES.Application.Tests.Validators;

/// <summary>US-12 AC2/AC15 — request lưu theo kết quả máy phải đủ bộ khóa chống trùng.</summary>
public class TesterScanValidatorTests
{
    private static CreateTesterScanRequest Valid() => new()
    {
        TagCode = "TAG1",
        TesterBootId = "8F21C04D",
        TesterSeq = 105,
        GoldenCrc = "3A7F",
        FirmwareVersion = "1.0.0",
        TestedAt = DateTime.Now,
    };

    [Fact]
    public void CreateTesterScan_DayDu_HopLe()
    {
        Assert.True(new CreateTesterScanRequestValidator().Validate(Valid()).IsValid);
    }

    [Fact]
    public void CreateTesterScan_GoldenCrc0000_VanHopLe()
    {
        var request = Valid();
        request.GoldenCrc = "0000";

        Assert.True(new CreateTesterScanRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("", "8F21C04D", 1)]
    [InlineData("TAG1", "", 1)]
    [InlineData("TAG1", "8F21C04D", 0)]
    [InlineData("TAG1", "8F21C04DX", 1)]
    public void CreateTesterScan_ThieuHoacSaiBoKhoa_BiTuChoi(string tag, string boot, int seq)
    {
        var request = Valid();
        request.TagCode = tag;
        request.TesterBootId = boot;
        request.TesterSeq = seq;

        Assert.False(new CreateTesterScanRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void CheckScan_ThieuMaTem_BiTuChoi()
    {
        Assert.False(new CheckScanRequestValidator().Validate(new CheckScanRequest { TagCode = " " }).IsValid);
        Assert.True(new CheckScanRequestValidator().Validate(new CheckScanRequest { TagCode = "TAG1" }).IsValid);
    }
}
