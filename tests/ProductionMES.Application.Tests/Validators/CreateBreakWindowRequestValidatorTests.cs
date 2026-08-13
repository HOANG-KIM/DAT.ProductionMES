using ProductionMES.Application.DTOs.BreakWindows;
using ProductionMES.Application.Validators.BreakWindows;

namespace ProductionMES.Application.Tests.Validators;

/// <summary>Unit test cho CreateBreakWindowRequestValidator, phần đầu AC5 của US-01a (EndTime &gt; StartTime).</summary>
public class CreateBreakWindowRequestValidatorTests
{
    private readonly CreateBreakWindowRequestValidator _sut = new();

    [Fact]
    public void Validate_GioKetThucLonHonGioBatDau_HopLe()
    {
        var request = new CreateBreakWindowRequest { StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0) };

        var result = _sut.Validate(request);

        Assert.True(result.IsValid);
    }

    // AC5 — Từ chối khi giờ kết thúc không lớn hơn giờ bắt đầu.
    [Theory]
    [InlineData(13, 0, 13, 0)]  // bằng nhau
    [InlineData(13, 0, 12, 0)]  // kết thúc trước bắt đầu
    public void Validate_GioKetThucKhongLonHonGioBatDau_KhongHopLe(int startHour, int startMinute, int endHour, int endMinute)
    {
        var request = new CreateBreakWindowRequest
        {
            StartTime = new TimeOnly(startHour, startMinute),
            EndTime = new TimeOnly(endHour, endMinute),
        };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBreakWindowRequest.EndTime));
    }
}
