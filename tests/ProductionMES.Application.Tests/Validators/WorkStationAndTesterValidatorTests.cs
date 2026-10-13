using ProductionMES.Application.DTOs.Testers;
using ProductionMES.Application.DTOs.WorkStations;
using ProductionMES.Application.Validators.Testers;
using ProductionMES.Application.Validators.WorkStations;

namespace ProductionMES.Application.Tests.Validators;

/// <summary>US-11 AC12 (không còn bắt buộc COM khi UseArduino = true) và AC4/AC5 (lý do đổi công tắc bắt buộc).</summary>
public class WorkStationAndTesterValidatorTests
{
    [Fact]
    public void CreateWorkStation_UseArduinoTrueKhongNhapCOM_HopLe()
    {
        var result = new CreateWorkStationRequestValidator().Validate(
            new CreateWorkStationRequest { Name = "Trạm TĐ", LineId = 1, StageId = 1, UseArduino = true });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateWorkStation_UseArduinoTrueKhongNhapCOM_HopLe()
    {
        var result = new UpdateWorkStationRequestValidator().Validate(
            new UpdateWorkStationRequest { Name = "Trạm TĐ", LineId = 1, StageId = 1, UseArduino = true });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateWorkStation_ThieuTen_VanBiTuChoi()
    {
        var result = new CreateWorkStationRequestValidator().Validate(
            new CreateWorkStationRequest { Name = "", LineId = 1, StageId = 1, UseArduino = true });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetTesterEnabled_ThieuLyDo_BiTuChoi()
    {
        var result = new SetTesterEnabledRequestValidator().Validate(
            new SetTesterEnabledRequest { WorkStationId = 1, Enabled = false, Reason = "  " });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetTesterEnabled_DayDu_HopLe()
    {
        var result = new SetTesterEnabledRequestValidator().Validate(
            new SetTesterEnabledRequest { WorkStationId = 1, Enabled = false, Reason = "Máy hỏng" });

        Assert.True(result.IsValid);
    }
}
