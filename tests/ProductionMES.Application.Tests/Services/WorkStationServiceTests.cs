using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.WorkStations;
using ProductionMES.Application.Services.WorkStations;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>Unit test cho WorkStationService, bám theo AC1-AC3 của US-04 (Documents/BACKLOG-user-story.md).</summary>
public class WorkStationServiceTests
{
    private readonly Mock<IRepository<WorkStation>> _tramRepositoryMock = new();
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly WorkStationService _sut;

    public WorkStationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_tramRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _sut = new WorkStationService(_unitOfWorkMock.Object);

        _lineRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Line { Id = 1, Name = "Line 1", IsActive = true });
        _stageRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stage { Id = 1, Name = "Thông điện", IsActive = true });
    }

    // AC1 — Tạo trạm làm việc: gắn đúng 1 Line và 1 công đoạn đã tồn tại.
    [Fact]
    public async Task CreateAsync_LineVaCongDoanTonTai_TaoTramGanDungLineVaCongDoan()
    {
        var request = new CreateWorkStationRequest { Name = "Trạm 1", LineId = 1, StageId = 1, UseArduino = false };

        var result = await _sut.CreateAsync(request);

        Assert.Equal(1, result.LineId);
        Assert.Equal(1, result.StageId);
        _tramRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WorkStation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_LineKhongTonTai_NemEntityNotFoundException()
    {
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Line?)null);
        var request = new CreateWorkStationRequest { Name = "Trạm 1", LineId = 99, StageId = 1, UseArduino = false };

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.CreateAsync(request));
    }

    // AC2 — Cấu hình cổng COM khi trạm có Arduino: thông tin cổng COM được lưu.
    [Fact]
    public async Task CreateAsync_SuDungArduinoTrue_LuuDayDuThongTinCongCOM()
    {
        var request = new CreateWorkStationRequest
        {
            Name = "Trạm Thông điện",
            LineId = 1,
            StageId = 1,
            UseArduino = true,
            ComPort = "COM3",
            BaudRate = 9600,
            CommandProtocol = "OK\\n",
        };

        var result = await _sut.CreateAsync(request);

        Assert.True(result.UseArduino);
        Assert.Equal("COM3", result.ComPort);
        Assert.Equal(9600, result.BaudRate);
        Assert.Equal("OK\\n", result.CommandProtocol);
    }

    // AC3 — Trạm không dùng Arduino không yêu cầu cấu hình COM: thông tin COM không được lưu (bỏ qua nếu có nhập).
    [Fact]
    public async Task CreateAsync_SuDungArduinoFalse_KhongLuuThongTinCongCOM()
    {
        var request = new CreateWorkStationRequest
        {
            Name = "Trạm thủ công",
            LineId = 1,
            StageId = 1,
            UseArduino = false,
            ComPort = "COM5", // dù có nhập, không dùng Arduino thì không lưu
        };

        var result = await _sut.CreateAsync(request);

        Assert.False(result.UseArduino);
        Assert.Null(result.ComPort);
        Assert.Null(result.BaudRate);
        Assert.Null(result.CommandProtocol);
    }

    [Fact]
    public async Task DeactivateAsync_TramDangHoatDong_ChuyenTrangThaiNgungHoatDong()
    {
        var existing = new WorkStation { Id = 1, Name = "Trạm 1", LineId = 1, StageId = 1, IsActive = true };
        _tramRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeactivateAsync(1);

        Assert.False(existing.IsActive);
        _tramRepositoryMock.Verify(r => r.Remove(It.IsAny<WorkStation>()), Times.Never);
    }
}
