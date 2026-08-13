using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.BreakWindows;
using ProductionMES.Application.Services.BreakWindows;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>Unit test cho BreakWindowService, bám theo AC1-AC5 của US-01a (Documents/BACKLOG-user-story.md).</summary>
public class BreakWindowServiceTests
{
    private readonly Mock<IRepository<BreakWindow>> _breakWindowRepositoryMock = new();
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly BreakWindowService _sut;

    public BreakWindowServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<BreakWindow>()).Returns(_breakWindowRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);
        _sut = new BreakWindowService(_unitOfWorkMock.Object);

        _lineRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Line { Id = 1, Name = "Line 1", IsActive = true });
    }

    // AC1 — Thêm khung giờ nghỉ cho Line: Line chưa có khung nào -> lưu thành công.
    [Fact]
    public async Task CreateAsync_LineChuaCoKhungGioNghiNao_LuuThanhCong()
    {
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BreakWindow>());

        var request = new CreateBreakWindowRequest
        {
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(13, 0),
            Note = "Nghỉ trưa",
        };

        var result = await _sut.CreateAsync(1, request);

        Assert.Equal(1, result.LineId);
        Assert.Equal(request.StartTime, result.StartTime);
        Assert.Equal(request.EndTime, result.EndTime);
        Assert.Equal("Nghỉ trưa", result.Note);
        _breakWindowRepositoryMock.Verify(r => r.AddAsync(It.IsAny<BreakWindow>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC2 — Line có thể có nhiều khung giờ nghỉ không chồng lấn nhau (nghỉ trưa + nghỉ giữa giờ).
    [Fact]
    public async Task CreateAsync_ThemKhungGioNghiThu2KhongChongLan_LuuThanhCong()
    {
        var existing = new List<BreakWindow>
        {
            new() { Id = 1, LineId = 1, StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0), Note = "Nghỉ trưa" },
        };
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateBreakWindowRequest
        {
            StartTime = new TimeOnly(15, 0),
            EndTime = new TimeOnly(15, 15),
            Note = "Nghỉ giữa giờ",
        };

        var result = await _sut.CreateAsync(1, request);

        Assert.Equal("Nghỉ giữa giờ", result.Note);
        _breakWindowRepositoryMock.Verify(r => r.AddAsync(It.IsAny<BreakWindow>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC5 — Từ chối khung giờ nghỉ chồng lấn 1 khung khác đã có của cùng Line.
    [Theory]
    [InlineData(12, 30, 12, 45)] // nằm hoàn toàn trong khung đã có (12:00-13:00)
    [InlineData(11, 30, 12, 30)] // chồng lấn phần đầu
    [InlineData(12, 30, 13, 30)] // chồng lấn phần cuối
    [InlineData(11, 0, 14, 0)]   // bao trùm toàn bộ khung đã có
    public async Task CreateAsync_ChongLanKhungGioNghiDaCo_NemBusinessRuleException(int startHour, int startMinute, int endHour, int endMinute)
    {
        var existing = new List<BreakWindow>
        {
            new() { Id = 1, LineId = 1, StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0), Note = "Nghỉ trưa" },
        };
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateBreakWindowRequest
        {
            StartTime = new TimeOnly(startHour, startMinute),
            EndTime = new TimeOnly(endHour, endMinute),
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(1, request));
        _breakWindowRepositoryMock.Verify(r => r.AddAsync(It.IsAny<BreakWindow>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC2 — Khung liền kề (không chồng lấn, chỉ chung biên) vẫn hợp lệ (13:00-13:00 không giao nhau theo nửa khoảng [s,e)).
    [Fact]
    public async Task CreateAsync_KhungGioLienKeKhongChongLan_LuuThanhCong()
    {
        var existing = new List<BreakWindow>
        {
            new() { Id = 1, LineId = 1, StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0) },
        };
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateBreakWindowRequest { StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(13, 15) };

        var result = await _sut.CreateAsync(1, request);

        Assert.Equal(new TimeOnly(13, 0), result.StartTime);
    }

    [Fact]
    public async Task CreateAsync_LineKhongTonTai_NemEntityNotFoundException()
    {
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Line?)null);

        var request = new CreateBreakWindowRequest { StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0) };

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.CreateAsync(99, request));
    }

    // AC3 — Sửa khung giờ nghỉ đã tồn tại.
    [Fact]
    public async Task UpdateAsync_KhungGioNghiDaTonTai_CapNhatThanhCong()
    {
        var existing = new BreakWindow { Id = 1, LineId = 1, StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0), Note = "Nghỉ trưa" };
        _breakWindowRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BreakWindow> { existing });

        var request = new UpdateBreakWindowRequest { StartTime = new TimeOnly(12, 15), EndTime = new TimeOnly(13, 15), Note = "Nghỉ trưa (đổi giờ)" };
        var result = await _sut.UpdateAsync(1, 1, request);

        Assert.Equal(new TimeOnly(12, 15), result.StartTime);
        Assert.Equal(new TimeOnly(13, 15), result.EndTime);
        Assert.Equal("Nghỉ trưa (đổi giờ)", result.Note);
        _breakWindowRepositoryMock.Verify(r => r.Update(existing), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC3 — Sửa khung giờ nghỉ không tự chồng lấn với chính nó (loại trừ Id đang sửa khỏi kiểm tra chồng lấn).
    [Fact]
    public async Task UpdateAsync_KhongDoiKhungGio_KhongBiCoiLaChongLanVoiChinhNo()
    {
        var existing = new BreakWindow { Id = 1, LineId = 1, StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0) };
        _breakWindowRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BreakWindow> { existing });

        var request = new UpdateBreakWindowRequest { StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0), Note = "Không đổi" };
        var result = await _sut.UpdateAsync(1, 1, request);

        Assert.Equal("Không đổi", result.Note);
    }

    // AC5 — Sửa khung giờ nghỉ chồng lấn khung khác đã có (khác Id) -> từ chối.
    [Fact]
    public async Task UpdateAsync_ChongLanKhungKhac_NemBusinessRuleException()
    {
        var target = new BreakWindow { Id = 1, LineId = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(9, 15) };
        var other = new BreakWindow { Id = 2, LineId = 1, StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0) };
        _breakWindowRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BreakWindow> { target, other });

        var request = new UpdateBreakWindowRequest { StartTime = new TimeOnly(12, 30), EndTime = new TimeOnly(12, 45) };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(1, 1, request));
    }

    [Fact]
    public async Task UpdateAsync_KhungGioNghiKhongTonTai_NemEntityNotFoundException()
    {
        _breakWindowRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((BreakWindow?)null);

        var request = new UpdateBreakWindowRequest { StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0) };

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.UpdateAsync(1, 99, request));
    }

    // AC3 — Xóa khung giờ nghỉ đã tồn tại.
    [Fact]
    public async Task DeleteAsync_KhungGioNghiDaTonTai_XoaThanhCong()
    {
        var existing = new BreakWindow { Id = 1, LineId = 1, StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0) };
        _breakWindowRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeleteAsync(1, 1);

        _breakWindowRepositoryMock.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_KhungGioNghiKhongTonTai_NemEntityNotFoundException()
    {
        _breakWindowRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((BreakWindow?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.DeleteAsync(1, 99));
    }

    // AC4 — Line không có khung giờ nghỉ nào vẫn hoạt động bình thường (trả danh sách rỗng, không lỗi).
    [Fact]
    public async Task GetByLineAsync_LineKhongCoKhungGioNghiNao_TraVeDanhSachRong()
    {
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BreakWindow>());

        var result = await _sut.GetByLineAsync(1);

        Assert.Empty(result);
    }
}
