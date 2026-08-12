using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Lines;
using ProductionMES.Application.Services.Lines;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho LineService, bám theo AC1-AC4 của US-01 (Documents/BACKLOG-user-story.md).
/// </summary>
public class LineServiceTests
{
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly LineService _sut;

    public LineServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);
        _sut = new LineService(_unitOfWorkMock.Object);
    }

    // AC1 — Thêm Line mới: nhập tên, mô tả và lưu -> tạo mới 1 Line với trạng thái hoạt động mặc định.
    [Fact]
    public async Task CreateAsync_NhapTenVaMoTaHopLe_TaoMoiLineVoiTrangThaiHoatDongMacDinh()
    {
        var request = new CreateLineRequest { Name = "Line 1", Description = "Dây chuyền lắp ráp số 1" };

        var result = await _sut.CreateAsync(request);

        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
        Assert.True(result.IsActive);
        _lineRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Line>(l => l.Name == request.Name && l.Description == request.Description && l.IsActive), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC4 — Bổ sung Line mới không cần deploy lại: Line mới có hiệu lực ngay (truy vấn lại thấy ngay, không cần bước nào thêm).
    [Fact]
    public async Task CreateAsync_SauKhiTaoLineMoi_LineMoiXuatHienNgayTrongDanhSach()
    {
        var request = new CreateLineRequest { Name = "Line 2" };
        Line? addedLine = null;
        _lineRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Line>(), It.IsAny<CancellationToken>()))
            .Callback<Line, CancellationToken>((line, _) => addedLine = line)
            .Returns(Task.CompletedTask);

        var created = await _sut.CreateAsync(request);

        // Giả lập: sau khi lưu, repository trả về danh sách đã bao gồm Line vừa tạo — không cần thêm bước cấu hình/deploy nào khác.
        _lineRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Line> { addedLine! });

        var all = await _sut.GetAllAsync();

        Assert.Contains(all, l => l.Id == created.Id && l.Name == request.Name);
    }

    // AC2 — Sửa thông tin Line: cập nhật tên/mô tả và lưu -> thông tin được cập nhật, không xóa/ảnh hưởng bản ghi.
    [Fact]
    public async Task UpdateAsync_LineDaTonTai_CapNhatTenVaMoTaThanhCong()
    {
        var existing = new Line { Id = 1, Name = "Line cũ", Description = "Mô tả cũ", IsActive = true };
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = new UpdateLineRequest { Name = "Line mới", Description = "Mô tả mới" };
        var result = await _sut.UpdateAsync(1, request);

        Assert.Equal("Line mới", result.Name);
        Assert.Equal("Mô tả mới", result.Description);
        Assert.True(result.IsActive); // không ảnh hưởng trạng thái hoạt động khi chỉ sửa tên/mô tả
        _lineRepositoryMock.Verify(r => r.Update(existing), Times.Once);
        _lineRepositoryMock.Verify(r => r.Remove(It.IsAny<Line>()), Times.Never); // không xóa cứng dữ liệu
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_LineKhongTonTai_NemEntityNotFoundException()
    {
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Line?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _sut.UpdateAsync(99, new UpdateLineRequest { Name = "Bất kỳ" }));
    }

    // AC3 — Vô hiệu hóa Line: Line đang hoạt động -> chọn vô hiệu hóa -> chuyển trạng thái ngưng hoạt động,
    // dữ liệu (bản ghi) vẫn được giữ nguyên, không xóa cứng (soft-delete).
    [Fact]
    public async Task DeactivateAsync_LineDangHoatDong_ChuyenTrangThaiNgungHoatDongVaKhongXoaCung()
    {
        var existing = new Line { Id = 1, Name = "Line 1", IsActive = true };
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeactivateAsync(1);

        Assert.False(existing.IsActive);
        _lineRepositoryMock.Verify(r => r.Update(existing), Times.Once);
        _lineRepositoryMock.Verify(r => r.Remove(It.IsAny<Line>()), Times.Never); // AC3: giữ nguyên dữ liệu lịch sử, không xóa cứng
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_LineKhongTonTai_NemEntityNotFoundException()
    {
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Line?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.DeactivateAsync(99));
    }

    [Fact]
    public async Task GetByIdAsync_LineKhongTonTai_TraVeNull()
    {
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Line?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.Null(result);
    }
}
