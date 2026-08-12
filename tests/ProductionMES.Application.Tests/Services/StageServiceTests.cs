using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Stages;
using ProductionMES.Application.Services.Stages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>Unit test cho StageService, bám theo AC1-AC3 của US-02 (Documents/BACKLOG-user-story.md).</summary>
public class StageServiceTests
{
    private readonly Mock<IRepository<Stage>> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly StageService _sut;

    public StageServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_repositoryMock.Object);
        _sut = new StageService(_unitOfWorkMock.Object);
    }

    // AC1 — Thêm công đoạn master: được thêm vào danh mục dùng chung, trạng thái hoạt động mặc định.
    [Fact]
    public async Task CreateAsync_NhapTenHopLe_TaoMoiCongDoanVoiTrangThaiHoatDongMacDinh()
    {
        var request = new CreateStageRequest { Name = "Lắp ráp", Description = "Công đoạn lắp ráp" };

        var result = await _sut.CreateAsync(request);

        Assert.Equal(request.Name, result.Name);
        Assert.True(result.IsActive);
        _repositoryMock.Verify(
            r => r.AddAsync(It.Is<Stage>(c => c.Name == request.Name && c.IsActive), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CongDoanDaTonTai_CapNhatTenVaMoTaThanhCong()
    {
        var existing = new Stage { Id = 1, Name = "Cũ", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(1, new UpdateStageRequest { Name = "Mới", Description = "Mô tả mới" });

        Assert.Equal("Mới", result.Name);
        _repositoryMock.Verify(r => r.Update(existing), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CongDoanKhongTonTai_NemEntityNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Stage?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _sut.UpdateAsync(99, new UpdateStageRequest { Name = "Bất kỳ" }));
    }

    // AC3 — Vô hiệu hóa công đoạn: chuyển trạng thái ngưng hoạt động, không xóa cứng dữ liệu.
    [Fact]
    public async Task DeactivateAsync_CongDoanDangHoatDong_ChuyenTrangThaiNgungHoatDongVaKhongXoaCung()
    {
        var existing = new Stage { Id = 1, Name = "Thông điện", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeactivateAsync(1);

        Assert.False(existing.IsActive);
        _repositoryMock.Verify(r => r.Update(existing), Times.Once);
        _repositoryMock.Verify(r => r.Remove(It.IsAny<Stage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CongDoanKhongTonTai_NemEntityNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Stage?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.DeactivateAsync(99));
    }

    // AC2 — Công đoạn dùng chung, không gắn cố định 1 Line: kiểm tra ở mức đơn giản là danh mục chỉ lưu 1
    // định danh (Id) duy nhất cho công đoạn, không có trường LineId nào trên Stage — đảm bảo qua thiết kế
    // entity (không có LineId), test này xác nhận GetAllAsync trả đúng danh mục dùng chung không phân biệt Line.
    [Fact]
    public async Task GetAllAsync_TraVeDanhMucDungChungKhongPhanBietLine()
    {
        var stages = new List<Stage>
        {
            new() { Id = 1, Name = "Thông điện", IsActive = true },
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(stages);

        var result = await _sut.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("Thông điện", result[0].Name);
    }
}
